using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Reads the "Repayment sheet" pivot matrix.
/// Layout: columns C4+ are members (R6=memberCode, R8=village-POC/staff, R17=weeklyEMI).
///         Rows R22+ are payment installments (C2=date, C3=installmentNo, C4+=amount paid).
///
/// For each paid installment:
///   1. Creates (or finds) a LoanScheduler entry.
///   2. LedgerTx: PaidFrom=NULL (member) → PaidTo=staff      type='Collection'
///   3. LedgerTx: PaidFrom=staff          → PaidTo=importUser type='Remittance'
///   4. Updates staff ledger (+then-) and importUser ledger (+).
/// </summary>
public class RepaymentImporter
{
    private readonly SqlConnection _conn;
    private readonly int _importUserId;

    // First column with member data
    private const int FirstMemberCol = 4;
    // Rows with specific metadata
    private const int RowMemberCode  = 6;
    private const int RowVillagePoc  = 8;
    private const int RowWeeklyEmi   = 17;   // "O/s weeks" → per-week outstanding amount
    private const int FirstDataRow   = 22;   // First row with actual payment data

    public RepaymentImporter(SqlConnection conn, int importUserId)
    {
        _conn         = conn;
        _importUserId = importUserId;
    }

    public async Task RunAsync(string filePath, string password)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);

        var sheet = pkg.Workbook.Worksheets["Repayment sheet"];
        if (sheet?.Dimension == null)
        {
            Console.WriteLine("[REPAYMENT] Sheet 'Repayment sheet' not found — skipped.");
            return;
        }

        Console.WriteLine($"[REPAYMENT] Reading sheet '{sheet.Name}'  rows={sheet.Dimension.Rows} cols={sheet.Dimension.Columns}");

        // ── 1. Build per-column member/staff metadata ─────────────────────────
        var columns = new List<ColMeta>();
        for (int c = FirstMemberCol; c <= sheet.Dimension.Columns; c++)
        {
            var code = sheet.Cells[RowMemberCode, c].Text?.Trim();
            if (string.IsNullOrWhiteSpace(code)) break;

            var villagePoc = sheet.Cells[RowVillagePoc, c].Text?.Trim() ?? "";
            // "CenterName-StaffName" — split on last '-'
            var dashIdx = villagePoc.LastIndexOf('-');
            var staffName = dashIdx >= 0 ? villagePoc[(dashIdx + 1)..].Trim() : "";

            var weeklyEmi = ParseDecimal(sheet.Cells[RowWeeklyEmi, c].Text);

            columns.Add(new ColMeta
            {
                Col       = c,
                MemberCode = code,
                StaffName  = staffName,
                WeeklyEmi  = weeklyEmi,
            });
        }
        Console.WriteLine($"[REPAYMENT] Found {columns.Count} member column(s).");

        // ── 2. Resolve member/loan/staff IDs from DB ──────────────────────────
        Console.WriteLine("[REPAYMENT] Resolving member/loan/staff IDs from DB...");
        var staffCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns)
        {
            col.MemberId = await GetMemberIdByCode(col.MemberCode);
            if (col.MemberId.HasValue)
                col.LoanId = await GetOpenLoanId(col.MemberId.Value);

            if (!string.IsNullOrWhiteSpace(col.StaffName))
            {
                if (!staffCache.TryGetValue(col.StaffName, out var sid))
                {
                    sid = await GetStaffUserId(col.StaffName);
                    staffCache[col.StaffName] = sid;
                }
                col.StaffUserId = sid;
            }
        }

        var validCols = columns.Where(c => c.MemberId.HasValue && c.LoanId.HasValue && c.StaffUserId.HasValue).ToList();
        Console.WriteLine($"[REPAYMENT] {validCols.Count}/{columns.Count} columns have member+loan+staff resolved.");

        if (validCols.Count == 0)
        {
            Console.WriteLine("[REPAYMENT] Nothing to import — done.");
            return;
        }

        // ── 3. Load loan details for principal/interest split ─────────────────
        var loanIds = validCols.Select(c => c.LoanId!.Value).Distinct().ToList();
        var loanDetails = await GetLoanDetails(loanIds);

        // ── 4. Process payment rows ────────────────────────────────────────────
        int schedulersCreated = 0, txsCreated = 0;

        for (int r = FirstDataRow; r <= sheet.Dimension.Rows; r++)
        {
            var dateText       = sheet.Cells[r, 2].Text?.Trim();
            var installmentText = sheet.Cells[r, 3].Text?.Trim();

            if (string.IsNullOrWhiteSpace(dateText)) continue;
            if (!int.TryParse(installmentText, out var installNo)) continue;

            var payDate = ParseDate(dateText);
            if (!payDate.HasValue) continue;

            foreach (var col in validCols)
            {
                var amount = ParseDecimal(sheet.Cells[r, col.Col].Text);
                if (amount <= 0) continue;

                var loanId     = col.LoanId!.Value;
                var staffId    = col.StaffUserId!.Value;

                // Get per-installment principal/interest from loan
                var (principal, interest) = SplitPrincipalInterest(amount, loanId, loanDetails);

                // 4a. Create or get LoanScheduler
                var schedulerId = await GetOrCreateSchedulerAsync(
                    loanId, payDate.Value, amount, principal, interest, installNo);
                schedulersCreated++;

                // 4b. LedgerTx: Member (NULL) → Staff
                var tx1 = await InsertLedgerTxAsync(
                    paidFrom: null, paidTo: staffId,
                    amount: amount, date: payDate.Value,
                    txType: "Collection", refId: schedulerId,
                    comments: $"EMI #{installNo} from member {col.MemberCode} to staff");
                await UpsertLedgerAsync(staffId, +amount);

                // 4c. LedgerTx: Staff → ImportUser
                var tx2 = await InsertLedgerTxAsync(
                    paidFrom: staffId, paidTo: _importUserId,
                    amount: amount, date: payDate.Value,
                    txType: "Remittance", refId: schedulerId,
                    comments: $"Staff remittance for EMI #{installNo} member {col.MemberCode}");
                await UpsertLedgerAsync(staffId, -amount);
                await UpsertLedgerAsync(_importUserId, +amount);

                txsCreated += 2;

                if (txsCreated % 200 == 0)
                    Console.WriteLine($"  ... {schedulersCreated} schedulers, {txsCreated} ledger txs so far...");
            }
        }

        Console.WriteLine($"[REPAYMENT] Done.  schedulers={schedulersCreated}  ledger txs={txsCreated}");
    }

    // ── DB helpers ────────────────────────────────────────────────────────────

    private async Task<int?> GetMemberIdByCode(string memberCode)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Members WHERE MemberCode = @code AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@code", memberCode);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? null : Convert.ToInt32(r);
    }

    private async Task<int?> GetOpenLoanId(int memberId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id FROM Loans WHERE MemberId = @mid AND IsDeleted = 0 ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("@mid", memberId);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? null : Convert.ToInt32(r);
    }

    private async Task<int?> GetStaffUserId(string staffName)
    {
        // Email: "zaiba begum" → "zaiba.begum.staff@navyafinservices.com"
        var emailPart = staffName.Trim().ToLowerInvariant().Replace(' ', '.');
        var email = $"{emailPart}.staff@navyafinservices.com";
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@email", email);
        var r = await cmd.ExecuteScalarAsync();
        if (r != null && r != DBNull.Value) return Convert.ToInt32(r);

        // Fallback: partial match on FirstName+LastName
        var parts = staffName.Trim().Split(' ', 2);
        using var cmd2 = _conn.CreateCommand();
        cmd2.CommandText = @"SELECT TOP 1 Id FROM Users
            WHERE FirstName = @fn AND (LastName = @ln OR @ln = '-')
            AND [Level] = 'Branch' AND IsDeleted = 0";
        cmd2.Parameters.AddWithValue("@fn", parts[0]);
        cmd2.Parameters.AddWithValue("@ln", parts.Length > 1 ? parts[1] : "-");
        var r2 = await cmd2.ExecuteScalarAsync();
        return r2 == null || r2 == DBNull.Value ? null : Convert.ToInt32(r2);
    }

    private async Task<Dictionary<int, (decimal LoanAmount, int NoOfTerms, decimal InterestAmount)>> GetLoanDetails(List<int> loanIds)
    {
        var result = new Dictionary<int, (decimal, int, decimal)>();
        if (loanIds.Count == 0) return result;
        var idList = string.Join(",", loanIds);
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, LoanAmount, NoOfTerms, InterestAmount FROM Loans WHERE Id IN ({idList})";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetInt32(0)] = (r.GetDecimal(1), r.GetInt32(2), r.GetDecimal(3));
        return result;
    }

    private async Task<int> GetOrCreateSchedulerAsync(
        int loanId, DateTime scheduleDate, decimal paymentAmount,
        decimal principal, decimal interest, int installNo)
    {
        // Idempotent: find existing scheduler for this loan + installment
        using var chk = _conn.CreateCommand();
        chk.CommandText = @"SELECT LoanSchedulerId FROM LoanSchedulers
                            WHERE LoanId = @loanId AND InstallmentNo = @installNo";
        chk.Parameters.AddWithValue("@loanId",     loanId);
        chk.Parameters.AddWithValue("@installNo",  installNo);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO LoanSchedulers
                (LoanId, ScheduleDate, PaymentDate, ActualEmiAmount,
                 ActualPrincipalAmount, ActualInterestAmount,
                 PaymentAmount, PrincipalAmount, InterestAmount,
                 InstallmentNo, Status, CollectedBy, CreatedBy, CreatedDate)
            OUTPUT INSERTED.LoanSchedulerId
            VALUES
                (@loanId, @schedDate, @payDate, @actualEmi,
                 @actualPrin, @actualInt,
                 @payAmt, @prin, @int,
                 @installNo, 'Paid', @collectedBy, @createdBy, GETUTCDATE())";
        ins.Parameters.AddWithValue("@loanId",      loanId);
        ins.Parameters.AddWithValue("@schedDate",   scheduleDate);
        ins.Parameters.AddWithValue("@payDate",     scheduleDate);
        ins.Parameters.AddWithValue("@actualEmi",   paymentAmount);
        ins.Parameters.AddWithValue("@actualPrin",  principal);
        ins.Parameters.AddWithValue("@actualInt",   interest);
        ins.Parameters.AddWithValue("@payAmt",      paymentAmount);
        ins.Parameters.AddWithValue("@prin",        principal);
        ins.Parameters.AddWithValue("@int",         interest);
        ins.Parameters.AddWithValue("@installNo",   installNo);
        ins.Parameters.AddWithValue("@collectedBy", _importUserId);
        ins.Parameters.AddWithValue("@createdBy",   _importUserId);
        return Convert.ToInt32(await ins.ExecuteScalarAsync());
    }

    private async Task<int> InsertLedgerTxAsync(
        int? paidFrom, int paidTo, decimal amount, DateTime date,
        string txType, int refId, string comments)
    {
        // Idempotent: skip if same tx already exists
        using var chk = _conn.CreateCommand();
        chk.CommandText = @"SELECT COUNT(1) FROM LedgerTransactions
            WHERE TransactionType = @type AND ReferenceId = @refId
              AND PaidToUserId = @paidTo
              AND (@paidFrom IS NULL AND PaidFromUserId IS NULL
                   OR PaidFromUserId = @paidFrom)";
        chk.Parameters.AddWithValue("@type",     txType);
        chk.Parameters.AddWithValue("@refId",    refId);
        chk.Parameters.AddWithValue("@paidTo",   paidTo);
        chk.Parameters.AddWithValue("@paidFrom", (object?)paidFrom ?? DBNull.Value);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) return 0;

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate,
                 CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            OUTPUT INSERTED.Id
            VALUES (@from, @to, @amount, @date,
                    @createdBy, GETUTCDATE(), @type, @refId, @comments)";
        ins.Parameters.AddWithValue("@from",      (object?)paidFrom ?? DBNull.Value);
        ins.Parameters.AddWithValue("@to",        paidTo);
        ins.Parameters.AddWithValue("@amount",    amount);
        ins.Parameters.AddWithValue("@date",      date);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        ins.Parameters.AddWithValue("@type",      txType);
        ins.Parameters.AddWithValue("@refId",     refId);
        ins.Parameters.AddWithValue("@comments",  comments);
        return Convert.ToInt32(await ins.ExecuteScalarAsync());
    }

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        using var r = await chk.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            var id      = r.GetInt32(0);
            var current = r.GetDecimal(1);
            r.Close();
            using var upd = _conn.CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", current + delta);
            upd.Parameters.AddWithValue("@id",     id);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            r.Close();
            using var ins = _conn.CreateCommand();
            ins.CommandText = "INSERT INTO Ledgers (UserId, Amount) VALUES (@uid, @amount)";
            ins.Parameters.AddWithValue("@uid",    userId);
            ins.Parameters.AddWithValue("@amount", delta);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static (decimal principal, decimal interest) SplitPrincipalInterest(
        decimal amount, int loanId,
        Dictionary<int, (decimal LoanAmount, int NoOfTerms, decimal InterestAmount)> loanDetails)
    {
        if (!loanDetails.TryGetValue(loanId, out var d) || d.NoOfTerms == 0)
            return (amount, 0m);

        var perTermPrincipal = Math.Round(d.LoanAmount / d.NoOfTerms, 2);
        var perTermInterest  = Math.Round(d.InterestAmount / d.NoOfTerms, 2);
        // Actual paid amount may differ (partial/full pay-off) — scale proportionally
        var total = perTermPrincipal + perTermInterest;
        if (total == 0) return (amount, 0m);
        var principal = Math.Round(amount * (perTermPrincipal / total), 2);
        var interest  = amount - principal;
        return (principal, interest);
    }

    private static decimal ParseDecimal(string? v)
    {
        if (string.IsNullOrWhiteSpace(v) || v == "-") return 0m;
        return decimal.TryParse(v.Replace(",", "").Trim(), out var d) ? d : 0m;
    }

    private static DateTime? ParseDate(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        // Formats seen: "10-Apr-26", "26.11.2025", "26/11/2025"
        if (DateTime.TryParseExact(v, new[]
        {
            "d-MMM-yy", "dd-MMM-yy", "d-MMM-yyyy", "dd-MMM-yyyy",
            "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy", "dd.MM.yyyy"
        }, System.Globalization.CultureInfo.InvariantCulture,
           System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    private class ColMeta
    {
        public int     Col        { get; set; }
        public string  MemberCode { get; set; } = "";
        public string  StaffName  { get; set; } = "";
        public decimal WeeklyEmi  { get; set; }
        public int?    MemberId   { get; set; }
        public int?    LoanId     { get; set; }
        public int?    StaffUserId { get; set; }
    }
}
