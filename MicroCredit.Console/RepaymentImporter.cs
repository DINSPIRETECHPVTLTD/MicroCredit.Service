using System.Data;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Reads the "Repayment sheet" pivot matrix and bulk-inserts all records.
///
/// Layout: columns C4+ = members (R6=memberCode), rows R22+ = installments.
/// R8 = "CenterName-POC" — used for display only, NOT for staff lookup.
/// The existing branch staff (created by BranchStaffImporter) is used as collector.
///
/// Strategy: load everything into DataTables in memory, then SqlBulkCopy in one
/// round-trip each — avoids thousands of individual remote DB calls.
/// </summary>
public class RepaymentImporter
{
    private readonly DbHelper _db;
    private readonly int _importUserId;
    private readonly int _orgId;

    private const int FirstMemberCol = 4;
    private const int RowMemberCode  = 6;
    private const int FirstDataRow   = 22;

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
        Console.WriteLine($"[REPAYMENT] Sheet rows={sheet.Dimension.Rows} cols={sheet.Dimension.Columns}");

        // ── 1. Load existing branch staff ─────────────────────────────────────
        var branchStaff = await GetBranchStaffAsync();
        if (branchStaff.Count == 0)
        {
            Console.WriteLine("[REPAYMENT] No branch staff found — run BranchStaffImporter first. Aborting.");
            return;
        }
        var collectorId = branchStaff[0].Id;
        Console.WriteLine($"[REPAYMENT] Collector: '{branchStaff[0].Name}' (id={collectorId})");

        // ── 2. Load member columns ────────────────────────────────────────────
        var memberCodes = new List<(int Col, string Code)>();
        for (int c = FirstMemberCol; c <= sheet.Dimension.Columns; c++)
        {
            var code = sheet.Cells[RowMemberCode, c].Text?.Trim();
            if (string.IsNullOrWhiteSpace(code)) break;
            memberCodes.Add((c, code));
        }
        Console.WriteLine($"[REPAYMENT] {memberCodes.Count} member columns.");

        // ── 3. Bulk-resolve member + loan IDs ─────────────────────────────────
        var codes      = memberCodes.Select(x => x.Code).Distinct().ToList();
        var memberMap  = await BulkGetMemberIds(codes);
        var memberIds  = memberMap.Values.Distinct().ToList();
        var loanMap    = await BulkGetLatestLoanIds(memberIds);
        var loanIds    = loanMap.Values.Distinct().ToList();
        var loanDetails = await BulkGetLoanDetails(loanIds);

        // Build per-column valid list
        var validCols = new List<(int Col, string Code, int MemberId, int LoanId)>();
        int noMember = 0, noLoan = 0;
        foreach (var (col, code) in memberCodes)
        {
            if (!memberMap.TryGetValue(code, out var memberId)) { noMember++; continue; }
            if (!loanMap.TryGetValue(memberId, out var loanId)) { noLoan++;   continue; }
            validCols.Add((col, code, memberId, loanId));
        }
        Console.WriteLine($"[REPAYMENT] Valid: {validCols.Count}/{memberCodes.Count}  (no-member={noMember}, no-loan={noLoan})");
        if (validCols.Count == 0) { Console.WriteLine("[REPAYMENT] Nothing to import."); return; }

        // ── 4. Load already-existing schedulers to skip duplicates ────────────
        var existingSchedulers = await GetExistingSchedulerKeys(loanIds); // HashSet<(loanId,installNo)>

        // ── 5. Load already-existing ledger tx keys to skip duplicates ─────────
        var existingTxKeys = await GetExistingLedgerTxKeys(loanIds); // HashSet<(type,loanId,installNoHint,paidTo)>

        // ── 6. Parse Excel → DataTables ───────────────────────────────────────
        var dtSchedulers = MakeSchedulerTable();
        var dtLedgerTx   = MakeLedgerTxTable();
        var ledgerDeltas = new Dictionary<int, decimal>(); // userId → net delta

        int schedulerRows = 0, txRows = 0;

        for (int r = FirstDataRow; r <= sheet.Dimension.Rows; r++)
        {
            var dateText        = sheet.Cells[r, 2].Text?.Trim();
            var installmentText = sheet.Cells[r, 3].Text?.Trim();
            if (string.IsNullOrWhiteSpace(dateText)) continue;
            if (!int.TryParse(installmentText, out var installNo)) continue;
            var payDate = ParseDate(dateText);
            if (!payDate.HasValue) continue;

            foreach (var (col, code, memberId, loanId) in validCols)
            {
                var amount = ParseDecimal(sheet.Cells[r, col].Text);
                if (amount <= 0) continue;

                var (principal, interest) = SplitPrincipalInterest(amount, loanId, loanDetails);
                var key = (loanId, installNo);

                // Scheduler
                if (!existingSchedulers.Contains(key))
                {
                    existingSchedulers.Add(key); // prevent duplicate within this batch
                    var row = dtSchedulers.NewRow();
                    row["LoanId"]                = loanId;
                    row["ScheduleDate"]          = payDate.Value;
                    row["PaymentDate"]           = payDate.Value;
                    row["ActualEmiAmount"]       = amount;
                    row["ActualPrincipalAmount"] = principal;
                    row["ActualInterestAmount"]  = interest;
                    row["PaymentAmount"]         = amount;
                    row["PrincipalAmount"]       = principal;
                    row["InterestAmount"]        = interest;
                    row["InstallmentNo"]         = installNo;
                    row["Status"]                = "Paid";
                    row["CollectedBy"]           = collectorId;
                    row["CreatedBy"]             = _importUserId;
                    row["CreatedDate"]           = DateTime.UtcNow;
                    dtSchedulers.Rows.Add(row);
                    schedulerRows++;
                }

                // Collection tx: NULL → collectorId
                var txKey1 = ("Collection", loanId, installNo, collectorId);
                if (!existingTxKeys.Contains(txKey1))
                {
                    existingTxKeys.Add(txKey1);
                    AddLedgerTxRow(dtLedgerTx, DBNull.Value, collectorId, amount, payDate.Value,
                        "Collection", loanId, $"EMI #{installNo} member {code}→staff");
                    txRows++;
                    ledgerDeltas[collectorId] = ledgerDeltas.GetValueOrDefault(collectorId) + amount;
                }

                // Remittance tx: collectorId → importUser
                var txKey2 = ("Remittance", loanId, installNo, _importUserId);
                if (!existingTxKeys.Contains(txKey2))
                {
                    existingTxKeys.Add(txKey2);
                    AddLedgerTxRow(dtLedgerTx, collectorId, _importUserId, amount, payDate.Value,
                        "Remittance", loanId, $"Staff remittance EMI #{installNo} member {code}");
                    txRows++;
                    ledgerDeltas[collectorId]   = ledgerDeltas.GetValueOrDefault(collectorId)   - amount;
                    ledgerDeltas[_importUserId] = ledgerDeltas.GetValueOrDefault(_importUserId) + amount;
                }
            }
        }

        Console.WriteLine($"[REPAYMENT] Prepared: {schedulerRows} schedulers, {txRows} ledger txs, {ledgerDeltas.Count} ledger balance update(s).");

        // ── 7. Bulk insert ────────────────────────────────────────────────────
        await using var conn = await _db.OpenAsync();
        conn.StatisticsEnabled = false;

        if (schedulerRows > 0)
        {
            Console.WriteLine("[REPAYMENT] Bulk inserting LoanSchedulers...");
            using var bc = new SqlBulkCopy(conn);
            bc.DestinationTableName = "LoanSchedulers";
            bc.BulkCopyTimeout      = 300;
            bc.BatchSize            = 1000;
            foreach (DataColumn col in dtSchedulers.Columns)
                bc.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            await bc.WriteToServerAsync(dtSchedulers);
            Console.WriteLine($"[REPAYMENT] LoanSchedulers inserted: {schedulerRows}");
        }

        if (txRows > 0)
        {
            Console.WriteLine("[REPAYMENT] Bulk inserting LedgerTransactions...");
            using var bc = new SqlBulkCopy(conn);
            bc.DestinationTableName = "LedgerTransactions";
            bc.BulkCopyTimeout      = 300;
            bc.BatchSize            = 1000;
            foreach (DataColumn col in dtLedgerTx.Columns)
                bc.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            await bc.WriteToServerAsync(dtLedgerTx);
            Console.WriteLine($"[REPAYMENT] LedgerTransactions inserted: {txRows}");
        }

        // ── 8. Apply ledger balance deltas ────────────────────────────────────
        if (ledgerDeltas.Count > 0)
        {
            Console.WriteLine("[REPAYMENT] Updating ledger balances...");
            foreach (var (userId, delta) in ledgerDeltas)
                await UpsertLedgerAsync(conn, userId, delta);
        }

        Console.WriteLine($"[REPAYMENT] Done.  schedulers={schedulerRows}  ledger txs={txRows}");
    }

    // ── Bulk DB lookups ────────────────────────────────────────────────────────

    private async Task<List<(int Id, string Name, string Email)>> GetBranchStaffAsync()
    {
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''), Email
                            FROM Users WHERE Role = 'Staff' AND [Level] = 'Branch'
                            AND IsDeleted = 0 AND OrgId = @orgId";
        cmd.Parameters.AddWithValue("@orgId", _orgId);
        var result = new List<(int, string, string)>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result.Add((r.GetInt32(0), r.GetString(1).Trim(), r.GetString(2)));
        return result;
    }

    private async Task<Dictionary<string, int>> BulkGetMemberIds(List<string> codes)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0) return result;
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        // Pass as comma-separated via temp table approach using VALUES
        var paramList = string.Join(",", codes.Select((_, i) => $"(@c{i})"));
        cmd.CommandText = $@"SELECT MemberCode, Id FROM Members
            WHERE IsDeleted = 0 AND MemberCode IN (SELECT v FROM (VALUES {paramList}) t(v))";
        for (int i = 0; i < codes.Count; i++)
            cmd.Parameters.AddWithValue($"@c{i}", codes[i]);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetString(0)] = r.GetInt32(1);
        return result;
    }

    private async Task<Dictionary<int, int>> BulkGetLatestLoanIds(List<int> memberIds)
    {
        var result = new Dictionary<int, int>();
        if (memberIds.Count == 0) return result;
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        var ids = string.Join(",", memberIds);
        cmd.CommandText = $@"SELECT MemberId, MAX(Id) FROM Loans
            WHERE IsDeleted = 0 AND MemberId IN ({ids}) GROUP BY MemberId";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetInt32(0)] = r.GetInt32(1);
        return result;
    }

    private async Task<Dictionary<int, (decimal LoanAmount, int NoOfTerms, decimal InterestAmount)>> BulkGetLoanDetails(List<int> loanIds)
    {
        var result = new Dictionary<int, (decimal, int, decimal)>();
        if (loanIds.Count == 0) return result;
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, LoanAmount, NoOfTerms, InterestAmount FROM Loans WHERE Id IN ({string.Join(",", loanIds)})";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetInt32(0)] = (r.GetDecimal(1), r.GetInt32(2), r.GetDecimal(3));
        return result;
    }

    private async Task<HashSet<(int loanId, int installNo)>> GetExistingSchedulerKeys(List<int> loanIds)
    {
        var result = new HashSet<(int, int)>();
        if (loanIds.Count == 0) return result;
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT LoanId, InstallmentNo FROM LoanSchedulers WHERE LoanId IN ({string.Join(",", loanIds)})";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result.Add((r.GetInt32(0), r.GetInt32(1)));
        return result;
    }

    private async Task<HashSet<(string type, int loanId, int installNo, int paidTo)>> GetExistingLedgerTxKeys(List<int> loanIds)
    {
        var result = new HashSet<(string, int, int, int)>();
        if (loanIds.Count == 0) return result;
        await using var conn = await _db.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT TransactionType, ReferenceId, PaidToUserId, Comments
            FROM LedgerTransactions
            WHERE TransactionType IN ('Collection','Remittance')
              AND ReferenceId IN ({string.Join(",", loanIds)})";
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var comments   = r.IsDBNull(3) ? "" : r.GetString(3);
            var installNo  = ExtractInstallNo(comments);
            result.Add((r.GetString(0), r.GetInt32(1), installNo, r.GetInt32(2)));
        }
        return result;
    }

    private static int ExtractInstallNo(string comments)
    {
        // "EMI #7 member ..." → 7
        var idx = comments.IndexOf("#", StringComparison.Ordinal);
        if (idx < 0) return -1;
        var rest = comments[(idx + 1)..];
        var spaceIdx = rest.IndexOf(' ');
        var numStr = spaceIdx >= 0 ? rest[..spaceIdx] : rest;
        return int.TryParse(numStr, out var n) ? n : -1;
    }

    // ── DataTable helpers ──────────────────────────────────────────────────────

    private static DataTable MakeSchedulerTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("LoanId",                typeof(int));
        dt.Columns.Add("ScheduleDate",           typeof(DateTime));
        dt.Columns.Add("PaymentDate",            typeof(DateTime));
        dt.Columns.Add("ActualEmiAmount",        typeof(decimal));
        dt.Columns.Add("ActualPrincipalAmount",  typeof(decimal));
        dt.Columns.Add("ActualInterestAmount",   typeof(decimal));
        dt.Columns.Add("PaymentAmount",          typeof(decimal));
        dt.Columns.Add("PrincipalAmount",        typeof(decimal));
        dt.Columns.Add("InterestAmount",         typeof(decimal));
        dt.Columns.Add("InstallmentNo",          typeof(int));
        dt.Columns.Add("Status",                 typeof(string));
        dt.Columns.Add("CollectedBy",            typeof(int));
        dt.Columns.Add("CreatedBy",              typeof(int));
        dt.Columns.Add("CreatedDate",            typeof(DateTime));
        return dt;
    }

    private static DataTable MakeLedgerTxTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("PaidFromUserId", typeof(int));
        dt.Columns.Add("PaidToUserId",   typeof(int));
        dt.Columns.Add("Amount",         typeof(decimal));
        dt.Columns.Add("PaymentDate",    typeof(DateTime));
        dt.Columns.Add("CreatedBy",      typeof(int));
        dt.Columns.Add("CreatedDate",    typeof(DateTime));
        dt.Columns.Add("TransactionType", typeof(string));
        dt.Columns.Add("ReferenceId",    typeof(int));
        dt.Columns.Add("Comments",       typeof(string));
        return dt;
    }

    private void AddLedgerTxRow(DataTable dt, object paidFrom, int paidTo,
        decimal amount, DateTime date, string txType, int refId, string comments)
    {
        var row = dt.NewRow();
        row["PaidFromUserId"]  = paidFrom;
        row["PaidToUserId"]    = paidTo;
        row["Amount"]          = amount;
        row["PaymentDate"]     = date;
        row["CreatedBy"]       = _importUserId;
        row["CreatedDate"]     = DateTime.UtcNow;
        row["TransactionType"] = txType;
        row["ReferenceId"]     = refId;
        row["Comments"]        = comments;
        dt.Rows.Add(row);
    }

    private static async Task UpsertLedgerAsync(SqlConnection conn, int userId, decimal delta)
    {
        using var chk = conn.CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        using var r = await chk.ExecuteReaderAsync();
        bool    found   = await r.ReadAsync();
        int     id      = found ? r.GetInt32(0)  : 0;
        decimal current = found ? r.GetDecimal(1) : 0m;
        r.Close();

        if (found)
        {
            using var upd = conn.CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @a WHERE Id = @id";
            upd.Parameters.AddWithValue("@a",  current + delta);
            upd.Parameters.AddWithValue("@id", id);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT INTO Ledgers (UserId, Amount) VALUES (@uid, @a)";
            ins.Parameters.AddWithValue("@uid", userId);
            ins.Parameters.AddWithValue("@a",   delta);
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
            "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy",   "dd.MM.yyyy",
            "M/d/yyyy", "MM/dd/yyyy"
        }, System.Globalization.CultureInfo.InvariantCulture,
           System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;
    }
}
