using System.Configuration;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;

public class ExcelImporter
{
    private readonly SqlConnection _conn;
    private readonly int _orgId;
    private readonly int _importUserId;
    private readonly int _investorUserId;

    public ExcelImporter(SqlConnection conn, int orgId, int importUserId, int investorUserId)
    {
        _conn = conn;
        _orgId = orgId;
        _importUserId = importUserId;
        _investorUserId = investorUserId;
    }

    public async Task RunAsync(string filePath, string password)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);
        var sheet = pkg.Workbook.Worksheets["Master Gruop"];

        if (sheet?.Dimension == null)
            throw new InvalidOperationException("Sheet 'Master Gruop' not found or is empty.");

        Console.WriteLine($"\n[IMPORT] Reading sheet '{sheet.Name}'  rows={sheet.Dimension.Rows}");

        // ── 1. Parse all valid rows ──────────────────────────────────────────
        var rows = ParseRows(sheet);
        Console.WriteLine($"[IMPORT] Parsed {rows.Count} data rows.");

        // ── 2. Get or create Branch ──────────────────────────────────────────
        var branchName = ConfigurationManager.AppSettings["Import.BranchName"]!;
        var branchId = await GetOrCreateBranchAsync(branchName);

        // ── 3. Get or create Centers (1 per unique village) ──────────────────
        var uniqueVillages = rows.Select(r => r.Village).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var centerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var village in uniqueVillages)
            centerMap[village] = await GetOrCreateCenterAsync(village, branchId);

        // ── 4. Get or create POCs (1 per unique Village+Staff combo) ─────────
        var pocMap = new Dictionary<(string village, string staff), int>(new VillageStaffComparer());
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Village) || string.IsNullOrWhiteSpace(row.HandledBy) || row.HandledBy == "--")
                continue;
            var key = (row.Village, row.HandledBy);
            if (!pocMap.ContainsKey(key))
            {
                var centerId = centerMap[row.Village];
                pocMap[key] = await GetOrCreatePocAsync(row.HandledBy, centerId);
            }
        }

        // ── 5. Import each member row ─────────────────────────────────────────
        int created = 0, skipped = 0, failed = 0;

        foreach (var row in rows)
        {
            try
            {
                if (!centerMap.TryGetValue(row.Village ?? "", out var centerId))
                {
                    Console.WriteLine($"  [SKIP] {row.MemberCode} — no center for village '{row.Village}'");
                    skipped++;
                    continue;
                }

                var pocKey = (row.Village!, row.HandledBy ?? "--");
                if (!pocMap.TryGetValue(pocKey, out var pocId))
                {
                    // Fall back to any POC in this center
                    pocId = await GetAnyPocInCenterAsync(centerId);
                    if (pocId == 0)
                    {
                        Console.WriteLine($"  [SKIP] {row.MemberCode} — no POC for center id={centerId}");
                        skipped++;
                        continue;
                    }
                }

                var memberId = await GetOrCreateMemberAsync(row, centerId, pocId);

                if (row.LoanAmount > 0)
                    await GetOrCreateLoanAsync(row, memberId);

                if (row.JoinFee > 0)
                    await GetOrCreateMembershipFeeAsync(memberId, row.JoinFee, row.DisbDate);

                created++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {row.MemberCode} — {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"\n[IMPORT] Done.  created/updated={created}  skipped={skipped}  errors={failed}");
    }

    // ── Parsers ──────────────────────────────────────────────────────────────

    private List<ExcelRow> ParseRows(ExcelWorksheet sheet)
    {
        var list = new List<ExcelRow>();
        for (int r = 6; r <= sheet.Dimension.Rows; r++)
        {
            var memberCode = Cell(sheet, r, 5);
            if (string.IsNullOrWhiteSpace(memberCode)) continue;

            // Split phone — cell sometimes has two numbers separated by newline
            var phones = Cell(sheet, r, 11)?.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            list.Add(new ExcelRow
            {
                RowNum     = r,
                JoiningDate = ParseDate(Cell(sheet, r, 3)),
                MemberCode = memberCode,
                MemberName = Cell(sheet, r, 6) ?? "",
                Age        = ParseInt(Cell(sheet, r, 7)),
                GuardianName  = Cell(sheet, r, 8) ?? "",
                GuardianAge   = ParseInt(Cell(sheet, r, 9)),
                Village    = Cell(sheet, r, 10)?.Trim() ?? "",
                Phone      = phones.Length > 0 ? phones[0].Trim() : "",
                AltPhone   = phones.Length > 1 ? phones[1].Trim() : null,
                DisbDate   = ParseDate(Cell(sheet, r, 12)) ?? DateTime.UtcNow,
                LoanAmount = ParseDecimal(Cell(sheet, r, 13)),
                JoinFee    = ParseDecimal(Cell(sheet, r, 19)),
                ProcessingFee    = ParseDecimal(Cell(sheet, r, 20)),
                InsuranceFee     = ParseDecimal(Cell(sheet, r, 21)),
                WeeklyDue  = ParseDecimal(Cell(sheet, r, 22)),
                Status     = Cell(sheet, r, 23) ?? "Active",
                HandledBy  = Cell(sheet, r, 24)?.Trim() ?? "--",
            });
        }
        return list;
    }

    private static string? Cell(ExcelWorksheet s, int r, int c) =>
        s.Cells[r, c].Text?.Trim().NullIfEmpty();

    private static DateTime? ParseDate(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        // Formats: "26.11.2025"  or  "26/11/2025"
        v = v.Replace(".", "/");
        return DateTime.TryParseExact(v, new[] { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : (DateTime?)null;
    }

    private static int ParseInt(string? v) =>
        int.TryParse(v?.Trim(), out var i) ? i : 0;

    private static decimal ParseDecimal(string? v) =>
        decimal.TryParse(v?.Trim(), out var d) ? d : 0m;

    // ── DB helpers ───────────────────────────────────────────────────────────

    private async Task<int> GetOrCreateBranchAsync(string name)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Branchs WHERE Name = @name AND OrgId = @orgId AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@orgId", _orgId);
        var existing = await cmd.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
        {
            var id = Convert.ToInt32(existing);
            Console.WriteLine($"[FOUND]   branch       => id={id} '{name}'");
            return id;
        }

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Branchs (Name, OrgId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@name, @orgId, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@name", name);
        ins.Parameters.AddWithValue("@orgId", _orgId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] branch       => id={newId} '{name}'");
        return newId;
    }

    private async Task<int> GetOrCreateCenterAsync(string village, int branchId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Centers WHERE Name = @name AND BranchId = @branchId AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@name", village);
        cmd.Parameters.AddWithValue("@branchId", branchId);
        var existing = await cmd.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Centers (Name, BranchId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@name, @branchId, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@name", village);
        ins.Parameters.AddWithValue("@branchId", branchId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] center       => id={newId} '{village}'");
        return newId;
    }

    private async Task<int> GetOrCreatePocAsync(string staffName, int centerId)
    {
        // Name: first word = FirstName, rest = LastName
        var parts = staffName.Trim().Split(' ', 2);
        var firstName = parts[0];
        var lastName  = parts.Length > 1 ? parts[1] : "-";

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM POCs WHERE FirstName = @fn AND LastName = @ln AND CenterId = @cid AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@fn", firstName);
        cmd.Parameters.AddWithValue("@ln", lastName);
        cmd.Parameters.AddWithValue("@cid", centerId);
        var existing = await cmd.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO POCs (FirstName, LastName, PhoneNumber, CenterId, CollectionFrequency, CollectionBy, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, '0000000000', @cid, 'Weekly', @createdBy, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn", firstName);
        ins.Parameters.AddWithValue("@ln", lastName);
        ins.Parameters.AddWithValue("@cid", centerId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] poc          => id={newId} '{staffName}' centerId={centerId}");
        return newId;
    }

    private async Task<int> GetAnyPocInCenterAsync(int centerId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id FROM POCs WHERE CenterId = @cid AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@cid", centerId);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
    }

    private async Task<int> GetOrCreateMemberAsync(ExcelRow row, int centerId, int pocId)
    {
        // Dedup by phone number
        if (!string.IsNullOrWhiteSpace(row.Phone))
        {
            using var chk = _conn.CreateCommand();
            chk.CommandText = "SELECT Id FROM Members WHERE PhoneNumber = @phone AND IsDeleted = 0";
            chk.Parameters.AddWithValue("@phone", row.Phone);
            var ex = await chk.ExecuteScalarAsync();
            if (ex != null && ex != DBNull.Value)
                return Convert.ToInt32(ex);
        }

        // Split member name
        var nameParts = row.MemberName.Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName  = nameParts.Length > 1 ? nameParts[1] : "-";

        // Split guardian name
        var gParts = row.GuardianName.Trim().Split(' ', 2);
        var gFirst = gParts[0];
        var gLast  = gParts.Length > 1 ? gParts[1] : "-";

        var phone    = string.IsNullOrWhiteSpace(row.Phone) ? "0000000000" : row.Phone;
        var altPhone = string.IsNullOrWhiteSpace(row.AltPhone) ? null : row.AltPhone;

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Members
                (FirstName, LastName, PhoneNumber, AltPhone, Address1, MemberCode,
                 Age, GuardianFirstName, GuardianLastName, GuardianPhone, GuardianAge,
                 CenterId, POCId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES
                (@fn, @ln, @phone, @altPhone, @village, @memberCode,
                 @age, @gfn, @gln, @gphone, @gage,
                 @centerId, @pocId, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn", firstName);
        ins.Parameters.AddWithValue("@ln", lastName);
        ins.Parameters.AddWithValue("@phone", phone);
        ins.Parameters.AddWithValue("@altPhone", (object?)altPhone ?? DBNull.Value);
        ins.Parameters.AddWithValue("@village", row.Village);
        ins.Parameters.AddWithValue("@memberCode", (object?)row.MemberCode.NullIfEmpty() ?? DBNull.Value);
        ins.Parameters.AddWithValue("@age", row.Age > 0 ? row.Age : 0);
        ins.Parameters.AddWithValue("@gfn", gFirst);
        ins.Parameters.AddWithValue("@gln", gLast);
        ins.Parameters.AddWithValue("@gphone", phone);               // fallback
        ins.Parameters.AddWithValue("@gage", row.GuardianAge > 0 ? row.GuardianAge : 0);
        ins.Parameters.AddWithValue("@centerId", centerId);
        ins.Parameters.AddWithValue("@pocId", pocId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var memberId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"  [CREATED] member {row.MemberCode} '{row.MemberName}' => id={memberId}");
        return memberId;
    }

    private async Task GetOrCreateLoanAsync(ExcelRow row, int memberId)
    {
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT COUNT(1) FROM Loans WHERE MemberId = @mid AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@mid", memberId);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) return;

        var totalAmount   = row.LoanAmount + row.ProcessingFee + row.InsuranceFee;
        var noOfTerms     = row.WeeklyDue > 0 ? (int)Math.Ceiling(row.LoanAmount / row.WeeklyDue) : 25;
        var status        = row.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ? "Closed" : "Active";
        var disbDate      = row.DisbDate;

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO Loans
                (MemberId, LoanAmount, InterestAmount, ProcessingFee, InsuranceFee,
                 IsSavingEnabled, SavingAmount, TotalAmount, Status,
                 DisbursementDate, CollectionStartDate, CollectionTerm, NoOfTerms,
                 CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES
                (@mid, @loanAmount, 0, @processingFee, @insuranceFee,
                 0, 0, @totalAmount, @status,
                 @disbDate, @disbDate, 'Weekly', @noOfTerms,
                 @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@mid", memberId);
        ins.Parameters.AddWithValue("@loanAmount", row.LoanAmount);
        ins.Parameters.AddWithValue("@processingFee", row.ProcessingFee);
        ins.Parameters.AddWithValue("@insuranceFee", row.InsuranceFee);
        ins.Parameters.AddWithValue("@totalAmount", totalAmount);
        ins.Parameters.AddWithValue("@status", status);
        ins.Parameters.AddWithValue("@disbDate", disbDate);
        ins.Parameters.AddWithValue("@noOfTerms", noOfTerms);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var loanId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"    [LOAN]  id={loanId} amount={row.LoanAmount:N0} status={status} terms={noOfTerms}");
    }

    private async Task GetOrCreateMembershipFeeAsync(int memberId, decimal amount, DateTime paidDate)
    {
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT COUNT(1) FROM MemberMembershipFees WHERE MemberId = @mid AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@mid", memberId);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) return;

        using var ins = _conn.CreateCommand();
        ins.CommandText = @"
            INSERT INTO MemberMembershipFees (MemberId, Amount, PaidDate, CollectedBy, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@mid, @amount, @paidDate, @collectedBy, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@mid", memberId);
        ins.Parameters.AddWithValue("@amount", amount);
        ins.Parameters.AddWithValue("@paidDate", paidDate);
        ins.Parameters.AddWithValue("@collectedBy", _importUserId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        await ins.ExecuteScalarAsync();
    }
}

// ── Data model ───────────────────────────────────────────────────────────────

public class ExcelRow
{
    public int    RowNum      { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string MemberCode  { get; set; } = "";
    public string MemberName  { get; set; } = "";
    public int    Age         { get; set; }
    public string GuardianName { get; set; } = "";
    public int    GuardianAge { get; set; }
    public string? Village    { get; set; }
    public string Phone       { get; set; } = "";
    public string? AltPhone   { get; set; }
    public DateTime DisbDate  { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal JoinFee    { get; set; }
    public decimal ProcessingFee  { get; set; }
    public decimal InsuranceFee   { get; set; }
    public decimal WeeklyDue  { get; set; }
    public string Status      { get; set; } = "Active";
    public string HandledBy   { get; set; } = "--";
}

public class VillageStaffComparer : IEqualityComparer<(string village, string staff)>
{
    public bool Equals((string village, string staff) x, (string village, string staff) y) =>
        StringComparer.OrdinalIgnoreCase.Equals(x.village, y.village) &&
        StringComparer.OrdinalIgnoreCase.Equals(x.staff, y.staff);
    public int GetHashCode((string village, string staff) obj) =>
        HashCode.Combine(obj.village.ToLowerInvariant(), obj.staff.ToLowerInvariant());
}

public static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
