using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Reads the "Repayment sheet" pivot matrix.
/// Layout: columns C4+ are members (R6=memberCode, R8=village-POC/staff).
///         Rows R22+ are payment installments (C2=date, C3=installmentNo, C4+=amount paid).
///
/// For each paid installment:
///   1. Creates (or finds) a LoanScheduler entry (status=Paid).
///   2. LedgerTx: NULL (member) → Staff      type='Collection'
///   3. LedgerTx: Staff        → ImportUser  type='Remittance'
///   4. Updates staff and importUser ledger balances.
///
/// Staff is resolved from the POC name in R8 ("Village-POCName").
/// If a POC name has no matching User, the user is created on-the-fly as Staff/Branch.
/// </summary>
public class RepaymentImporter
{
    private readonly DbHelper _db;
    private readonly int _importUserId;
    private readonly int _orgId;

    private const int FirstMemberCol = 4;
    private const int RowMemberCode  = 6;
    private const int RowVillagePoc  = 8;
    private const int FirstDataRow   = 22;

    private const string DefaultPassword = "N@VY@$y$t3m001";
    private const string EmailDomain     = "navyafinservices.com";

    public RepaymentImporter(DbHelper db, int importUserId, int orgId)
    {
        _db           = db;
        _importUserId = importUserId;
        _orgId        = orgId;
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
            var dashIdx   = villagePoc.LastIndexOf('-');
            var staffName = dashIdx >= 0 ? villagePoc[(dashIdx + 1)..].Trim() : "";

            columns.Add(new ColMeta
            {
                Col        = c,
                MemberCode = code,
                StaffName  = staffName,
            });
        }
        Console.WriteLine($"[REPAYMENT] Found {columns.Count} member column(s).");

        // ── 2. Resolve member/loan/staff IDs from DB ──────────────────────────
        Console.WriteLine("[REPAYMENT] Resolving member/loan/staff IDs from DB...");

        int noMember = 0, noLoan = 0, noStaff = 0;
        var staffCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in columns)
        {
            col.MemberId = await GetMemberIdByCode(col.MemberCode);
            if (!col.MemberId.HasValue) { noMember++; continue; }

            col.LoanId = await GetLatestLoanId(col.MemberId.Value);
            if (!col.LoanId.HasValue) { noLoan++; continue; }

            if (!string.IsNullOrWhiteSpace(col.StaffName))
            {
                if (!staffCache.TryGetValue(col.StaffName, out var sid))
                {
                    sid = await GetOrCreateStaffUserAsync(col.StaffName);
                    staffCache[col.StaffName] = sid;
                }
                col.StaffUserId = sid;
            }
            else
            {
                col.StaffUserId = _importUserId;
            }

            if (col.StaffUserId == 0) noStaff++;
        }

        var validCols = columns
            .Where(c => c.MemberId.HasValue && c.LoanId.HasValue && c.StaffUserId.HasValue && c.StaffUserId > 0)
            .ToList();

        Console.WriteLine($"[REPAYMENT] Resolved: {validCols.Count}/{columns.Count} valid  " +
                          $"(no-member={noMember}, no-loan={noLoan}, no-staff={noStaff})");

        if (validCols.Count == 0)
        {
            Console.WriteLine("[REPAYMENT] Nothing to import — done.");
            return;
        }

        // ── 3. Load loan details for principal/interest split ─────────────────
        var loanIds    = validCols.Select(c => c.LoanId!.Value).Distinct().ToList();
        var loanDetails = await GetLoanDetailsAsync(loanIds);

        // ── 4. Process payment rows ────────────────────────────────────────────
        int schedulersCreated = 0, txsCreated = 0;

        for (int r = FirstDataRow; r <= sheet.Dimension.Rows; r++)
        {
            var dateText        = sheet.Cells[r, 2].Text?.Trim();
            var installmentText = sheet.Cells[r, 3].Text?.Trim();

            if (string.IsNullOrWhiteSpace(dateText)) continue;
            if (!int.TryParse(installmentText, out var installNo)) continue;

            var payDate = ParseDate(dateText);
            if (!payDate.HasValue) continue;

            foreach (var col in validCols)
            {
                var amount = ParseDecimal(sheet.Cells[r, col.Col].Text);
                if (amount <= 0) continue;

                var loanId  = col.LoanId!.Value;
                var staffId = col.StaffUserId!.Value;

                var (principal, interest) = SplitPrincipalInterest(amount, loanId, loanDetails);

                // 4a. Create or find LoanScheduler
                var schedulerId = await GetOrCreateSchedulerAsync(
                    loanId, payDate.Value, amount, principal, interest, installNo, staffId);
                schedulersCreated++;

                // 4b. Member (NULL) → Staff  [Collection]
                await InsertLedgerTxIfNotExistsAsync(
                    paidFrom: null, paidTo: staffId,
                    amount, payDate.Value, "Collection", schedulerId,
                    $"EMI #{installNo} member {col.MemberCode}→staff");
                await UpsertLedgerAsync(staffId, +amount);

                // 4c. Staff → ImportUser  [Remittance]
                await InsertLedgerTxIfNotExistsAsync(
                    paidFrom: staffId, paidTo: _importUserId,
                    amount, payDate.Value, "Remittance", schedulerId,
                    $"Staff remittance EMI #{installNo} member {col.MemberCode}");
                await UpsertLedgerAsync(staffId, -amount);
                await UpsertLedgerAsync(_importUserId, +amount);

                txsCreated += 2;
            }

            if (schedulersCreated > 0 && schedulersCreated % 500 == 0)
                Console.WriteLine($"  ... {schedulersCreated} schedulers, {txsCreated} ledger txs so far...");
        }

        Console.WriteLine($"[REPAYMENT] Done.  schedulers={schedulersCreated}  ledger txs={txsCreated}");
    }

    // ── DB helpers ────────────────────────────────────────────────────────────

    private async Task<int?> GetMemberIdByCode(string memberCode)
    {
        var conn = await _db.GetConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Members WHERE MemberCode = @code AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@code", memberCode);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? null : Convert.ToInt32(r);
    }

    private async Task<int?> GetLatestLoanId(int memberId)
    {
        var conn = await _db.GetConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id FROM Loans WHERE MemberId = @mid AND IsDeleted = 0 ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("@mid", memberId);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? null : Convert.ToInt32(r);
    }

    private async Task<int> GetOrCreateStaffUserAsync(string fullName)
    {
        var emailPart = fullName.Trim().ToLowerInvariant().Replace(' ', '.');
        var email     = $"{emailPart}.staff@{EmailDomain}";

        var conn = await _db.GetConn();

        using var chk = conn.CreateCommand();
        chk.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@email", email);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        var parts = fullName.Trim().Split(' ', 2);
        using var chk2 = conn.CreateCommand();
        chk2.CommandText = "SELECT TOP 1 Id FROM Users WHERE FirstName = @fn AND IsDeleted = 0 AND [Level] = 'Branch'";
        chk2.Parameters.AddWithValue("@fn", parts[0]);
        var existing2 = await chk2.ExecuteScalarAsync();
        if (existing2 != null && existing2 != DBNull.Value)
            return Convert.ToInt32(existing2);

        var pwdHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        using var ins = conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Users (FirstName, LastName, Email, PasswordHash, Role, [Level], OrgId, BranchId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, @email, @pwd, 'Staff', 'Branch', @orgId, NULL, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn",        parts[0]);
        ins.Parameters.AddWithValue("@ln",        parts.Length > 1 ? parts[1] : "-");
        ins.Parameters.AddWithValue("@email",     email);
        ins.Parameters.AddWithValue("@pwd",       pwdHash);
        ins.Parameters.AddWithValue("@orgId",     _orgId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"  [STAFF-CREATED] '{fullName}'  email={email}  id={newId}");
        return newId;
    }

    private async Task<Dictionary<int, (decimal LoanAmount, int NoOfTerms, decimal InterestAmount)>> GetLoanDetailsAsync(List<int> loanIds)
    {
        var result = new Dictionary<int, (decimal, int, decimal)>();
        if (loanIds.Count == 0) return result;
        var conn = await _db.GetConn();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, LoanAmount, NoOfTerms, InterestAmount FROM Loans WHERE Id IN ({string.Join(",", loanIds)})";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetInt32(0)] = (r.GetDecimal(1), r.GetInt32(2), r.GetDecimal(3));
        return result;
    }

    private async Task<int> GetOrCreateSchedulerAsync(
        int loanId, DateTime scheduleDate, decimal paymentAmount,
        decimal principal, decimal interest, int installNo, int collectedBy)
    {
        var conn = await _db.GetConn();

        using var chk = conn.CreateCommand();
        chk.CommandText = "SELECT LoanSchedulerId FROM LoanSchedulers WHERE LoanId = @loanId AND InstallmentNo = @installNo";
        chk.Parameters.AddWithValue("@loanId",    loanId);
        chk.Parameters.AddWithValue("@installNo", installNo);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        var conn2 = await _db.GetConn();
        using var ins = conn2.CreateCommand();
        ins.CommandText = @"
            INSERT INTO LoanSchedulers
                (LoanId, ScheduleDate, PaymentDate, ActualEmiAmount, ActualPrincipalAmount, ActualInterestAmount,
                 PaymentAmount, PrincipalAmount, InterestAmount, InstallmentNo, Status, CollectedBy, CreatedBy, CreatedDate)
            OUTPUT INSERTED.LoanSchedulerId
            VALUES
                (@loanId, @schedDate, @payDate, @actualEmi, @actualPrin, @actualInt,
                 @payAmt, @prin, @int, @installNo, 'Paid', @collectedBy, @createdBy, GETUTCDATE())";
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
        ins.Parameters.AddWithValue("@collectedBy", collectedBy);
        ins.Parameters.AddWithValue("@createdBy",   _importUserId);
        return Convert.ToInt32(await ins.ExecuteScalarAsync());
    }

    private async Task InsertLedgerTxIfNotExistsAsync(
        int? paidFrom, int paidTo, decimal amount, DateTime date,
        string txType, int refId, string comments)
    {
        var conn = await _db.GetConn();

        using var chk = conn.CreateCommand();
        chk.CommandText = @"SELECT COUNT(1) FROM LedgerTransactions
            WHERE TransactionType = @type AND ReferenceId = @refId AND PaidToUserId = @paidTo
              AND ((@paidFrom IS NULL AND PaidFromUserId IS NULL) OR PaidFromUserId = @paidFrom)";
        chk.Parameters.AddWithValue("@type",     txType);
        chk.Parameters.AddWithValue("@refId",    refId);
        chk.Parameters.AddWithValue("@paidTo",   paidTo);
        chk.Parameters.AddWithValue("@paidFrom", (object?)paidFrom ?? DBNull.Value);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) return;

        var conn2 = await _db.GetConn();
        using var ins = conn2.CreateCommand();
        ins.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            VALUES (@from, @to, @amount, @date, @createdBy, GETUTCDATE(), @type, @refId, @comments)";
        ins.Parameters.AddWithValue("@from",      (object?)paidFrom ?? DBNull.Value);
        ins.Parameters.AddWithValue("@to",        paidTo);
        ins.Parameters.AddWithValue("@amount",    amount);
        ins.Parameters.AddWithValue("@date",      date);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        ins.Parameters.AddWithValue("@type",      txType);
        ins.Parameters.AddWithValue("@refId",     refId);
        ins.Parameters.AddWithValue("@comments",  comments);
        await ins.ExecuteNonQueryAsync();
    }

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
        var conn = await _db.GetConn();

        using var chk = conn.CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        using var r = await chk.ExecuteReaderAsync();
        bool found       = await r.ReadAsync();
        int ledgerId     = found ? r.GetInt32(0) : 0;
        decimal current  = found ? r.GetDecimal(1) : 0m;
        r.Close();

        var conn2 = await _db.GetConn();
        if (found)
        {
            using var upd = conn2.CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", current + delta);
            upd.Parameters.AddWithValue("@id",     ledgerId);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            using var ins = conn2.CreateCommand();
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
        var total = d.LoanAmount + d.InterestAmount;
        if (total == 0) return (amount, 0m);
        var principal = Math.Round(amount * (d.LoanAmount / total), 2);
        return (principal, amount - principal);
    }

    private static decimal ParseDecimal(string? v)
    {
        if (string.IsNullOrWhiteSpace(v) || v == "-") return 0m;
        return decimal.TryParse(v.Replace(",", "").Trim(), out var d) ? d : 0m;
    }

    private static DateTime? ParseDate(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        return DateTime.TryParseExact(v, new[]
        {
            "d-MMM-yy", "dd-MMM-yy", "d-MMM-yyyy", "dd-MMM-yyyy",
            "d/M/yyyy",  "dd/MM/yyyy", "d.M.yyyy",   "dd.MM.yyyy",
            "M/d/yyyy",  "MM/dd/yyyy"
        }, System.Globalization.CultureInfo.InvariantCulture,
           System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;
    }

    private class ColMeta
    {
        public int     Col         { get; set; }
        public string  MemberCode  { get; set; } = "";
        public string  StaffName   { get; set; } = "";
        public int?    MemberId    { get; set; }
        public int?    LoanId      { get; set; }
        public int?    StaffUserId { get; set; }
    }
}
