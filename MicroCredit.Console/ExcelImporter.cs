using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;

/// <summary>
/// Imports everything from the "Master Gruop" tab.
///
/// Column 10 ("Village") = "CenterName-POCName" — split into Center + POC.
/// Column 24 ("Handled By") = the Staff name — a Staff User is created/found per
/// unique value and used wherever a collector/CollectedBy/ledger user is needed.
///
///   - Branch / Centers / POCs (POCs.CollectionBy = the Handled-By staff user)
///   - Members
///   - Loans: LoanAmount = "B/F Loan Amount", ProcessingFee = 3% of LoanAmount (computed),
///     InsuranceFee read from Excel, DisbursementDate = "Disb date".
///     Money now flows entirely through branch staff (not ImportUser):
///     investments are received by staff, staff funds the loan, staff collects EMI.
///     Ledger tx: PaidFrom=Handled-By staff, PaidTo=NULL (member side, no User row exists
///     for Members), type='Loan disbursement'.
///   - Membership fee: fixed 300, recorded in MemberMembershipFees + a LedgerTransaction
///     (PaidFrom=NULL, PaidTo=Handled-By staff user, type='Member Joining Fee') —
///     matches the live app's MemberMembershipFeeService convention.
///   - LoanSchedulers: generated weekly from DisbursementDate (collection day = same
///     weekday as disbursement), for NoOfTerms installments.
///     weeksOutstanding = ceil("Out" column / WeeklyDue); paidWeeks = NoOfTerms - weeksOutstanding.
///     If Status="Closed", the loan is fully paid off (paidWeeks = NoOfTerms).
///     For each paid installment: LoanScheduler.Status='Paid' + ONE ledger tx
///     (PaidFrom=NULL→Staff type='EMI Recovery') — money stays with staff, no further
///     hop to ImportUser.
///
///   ImportUser is still created/used only as CreatedBy/audit user — no money routes
///   through it anymore.
/// </summary>
public class ExcelImporter
{
    private readonly DbHelper _db;
    private readonly int _orgId;
    private readonly int _importUserId;

    private const decimal MembershipFeeAmount = 300m;
    private const decimal ProcessingFeeRate   = 0.03m;
    private const string EmailDomain          = "navyafinservices.com";
    private const string DefaultPassword      = "N@VY@$y$t3m001";

    public ExcelImporter(DbHelper db, int orgId, int importUserId)
    {
        _db = db;
        _orgId = orgId;
        _importUserId = importUserId;
    }

    /// <summary>
    /// Lightweight pre-pass: creates/gets the Branch and the first unique "Handled By"
    /// staff user from "Master Gruop". Used so RemittanceCreditsImporter can route
    /// investment money to the branch staff before the full member/loan import runs.
    /// Idempotent — safe to call again from RunAsync.
    /// </summary>
    public async Task<int> GetPrimaryStaffUserIdAsync(string filePath, string password)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(filePath), password);
        var sheet = pkg.Workbook.Worksheets["Master Gruop"];
        if (sheet?.Dimension == null)
            throw new InvalidOperationException("Sheet 'Master Gruop' not found or is empty.");

        var branchName = ConfigurationManager.AppSettings["Import.BranchName"]!;
        var branchId = await GetOrCreateBranchAsync(branchName);

        for (int r = 6; r <= sheet.Dimension.Rows; r++)
        {
            var handledBy = Cell(sheet, r, 24);
            if (string.IsNullOrWhiteSpace(handledBy) || handledBy == "--") continue;
            return await GetOrCreateStaffUserAsync(handledBy, branchId);
        }

        throw new InvalidOperationException("No 'Handled By' staff name found in 'Master Gruop' — cannot resolve a branch staff recipient for investments.");
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

        // ── 4. Get or create a Staff User per unique "Handled By" name ────────
        var uniqueStaffNames = rows.Select(r => r.HandledBy).Where(s => !string.IsNullOrWhiteSpace(s) && s != "--").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var staffMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in uniqueStaffNames)
            staffMap[name] = await GetOrCreateStaffUserAsync(name, branchId);
        Console.WriteLine($"[IMPORT] {staffMap.Count} 'Handled By' staff user(s) resolved.");

        // ── 5. Get or create POCs (1 per unique Village+PocName combo) ────────
        var pocMap = new Dictionary<(string village, string poc), int>(new VillageStaffComparer());
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Village) || string.IsNullOrWhiteSpace(row.PocName))
                continue;
            var key = (row.Village, row.PocName);
            if (!pocMap.ContainsKey(key))
            {
                var centerId = centerMap[row.Village];
                var staffUserId = (!string.IsNullOrWhiteSpace(row.HandledBy) && row.HandledBy != "--" && staffMap.TryGetValue(row.HandledBy, out var sid))
                    ? sid : _importUserId;
                pocMap[key] = await GetOrCreatePocAsync(row.PocName, centerId, staffUserId);
            }
        }

        // ── 6. Import each member row ─────────────────────────────────────────
        int created = 0, skipped = 0, failed = 0;
        var schedulerTable = MakeSchedulerTable();
        var ledgerTxTable  = MakeLedgerTxTable();
        var ledgerDeltas   = new Dictionary<int, decimal>();
        decimal totalInsuranceFee = 0m, totalProcessingFee = 0m, totalJoiningFee = 0m;

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

                var pocKey = (row.Village!, row.PocName ?? "--");
                int pocId;
                int staffUserId = (!string.IsNullOrWhiteSpace(row.HandledBy) && row.HandledBy != "--" && staffMap.TryGetValue(row.HandledBy, out var resolvedSid))
                    ? resolvedSid : _importUserId;

                if (!pocMap.TryGetValue(pocKey, out pocId))
                {
                    pocId = await GetAnyPocInCenterAsync(centerId);
                    if (pocId == 0)
                    {
                        Console.WriteLine($"  [SKIP] {row.MemberCode} — no POC for center id={centerId}");
                        skipped++;
                        continue;
                    }
                }

                var memberId = await GetOrCreateMemberAsync(row, centerId, pocId);

                int? loanId = null;
                bool loanIsNew = false;
                int noOfTerms = 0;
                if (row.LoanAmount > 0)
                {
                    var (lid, isNew, terms, processingFee) = await GetOrCreateLoanAsync(row, memberId, staffUserId);
                    loanId = lid;
                    loanIsNew = isNew;
                    noOfTerms = terms;

                    if (isNew)
                    {
                        // Branch staff funds the loan — money flows through staff, not ImportUser.
                        ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) - row.LoanAmount;
                        // Fees are NOT written to LedgerTransactions — only accumulated in
                        // Insurance_Claim_Financial_Summary (matches LoansService convention).
                        totalInsuranceFee  += row.InsuranceFee;
                        totalProcessingFee += processingFee;
                    }
                }

                // Fixed membership fee + ledger tx (NULL → Handled-By staff)
                var joinDate = row.JoiningDate ?? row.DisbDate;
                var feeCreated = await GetOrCreateMembershipFeeAndLedgerAsync(memberId, joinDate, staffUserId);
                if (feeCreated)
                {
                    ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) + MembershipFeeAmount;
                    totalJoiningFee += MembershipFeeAmount;
                }

                if (loanId.HasValue && loanIsNew)
                    QueueSchedulersAndPayments(schedulerTable, ledgerTxTable, ledgerDeltas,
                        loanId.Value, memberId, row.DisbDate, row.LoanAmount, row.WeeklyDue,
                        noOfTerms, row.OutstandingAmount, row.Status, staffUserId);

                created++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {row.MemberCode} — {ex.Message}");
                failed++;
            }
        }

        // ── 7. Bulk insert generated LoanSchedulers ────────────────────────────
        if (schedulerTable.Rows.Count > 0)
        {
            Console.WriteLine($"[IMPORT] Bulk inserting {schedulerTable.Rows.Count} LoanSchedulers...");
            await using var conn = await _db.OpenAsync();
            using var bc = new SqlBulkCopy(conn) { DestinationTableName = "LoanSchedulers", BulkCopyTimeout = 300, BatchSize = 1000 };
            foreach (DataColumn col in schedulerTable.Columns)
                bc.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            await bc.WriteToServerAsync(schedulerTable);
            Console.WriteLine($"[IMPORT] LoanSchedulers inserted: {schedulerTable.Rows.Count}");
        }

        // ── 8. Bulk insert paid-installment LedgerTransactions ─────────────────
        if (ledgerTxTable.Rows.Count > 0)
        {
            Console.WriteLine($"[IMPORT] Bulk inserting {ledgerTxTable.Rows.Count} LedgerTransactions (EMI Recovery)...");
            await using var conn = await _db.OpenAsync();
            using var bc = new SqlBulkCopy(conn) { DestinationTableName = "LedgerTransactions", BulkCopyTimeout = 300, BatchSize = 1000 };
            foreach (DataColumn col in ledgerTxTable.Columns)
                bc.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            await bc.WriteToServerAsync(ledgerTxTable);
            Console.WriteLine($"[IMPORT] LedgerTransactions inserted: {ledgerTxTable.Rows.Count}");
        }

        // ── 9. Apply all accumulated ledger balance deltas ─────────────────────
        foreach (var (userId, delta) in ledgerDeltas)
            await UpsertLedgerAsync(userId, delta);

        // ── 10. Accumulate Insurance_Claim_Financial_Summary totals ────────────
        // Fees are NOT written to LedgerTransactions — only accumulated here,
        // matching LoansService.AccumulateLoanCreationTotalsAsync / MemberMembershipFeeService.AccumulateJoiningFeeAsync.
        if (totalInsuranceFee > 0 || totalProcessingFee > 0 || totalJoiningFee > 0)
        {
            await AccumulateFinancialSummaryAsync(totalInsuranceFee, totalProcessingFee, totalJoiningFee);
            Console.WriteLine($"[IMPORT] Insurance_Claim_Financial_Summary updated: +InsuranceFee={totalInsuranceFee:N0}  +ProcessingFee={totalProcessingFee:N0}  +JoiningFee={totalJoiningFee:N0}");
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

            // C10 ("Village") format: "CenterName-POCName"  (first part = Center, second part = POC)
            var villageParts = (Cell(sheet, r, 10) ?? "").Split('-', 2);
            var village = villageParts[0].Trim();
            var pocName = villageParts.Length > 1 ? villageParts[1].Trim() : "--";

            // C24 = "Handled By" — the Staff name
            var handledBy = Cell(sheet, r, 24) ?? "--";

            // Loan amount: "B/F Loan Amount" (C13) when set, else fall back to
            // 1st Loan (C14) → 2nd Loan (C15) → 3rd Loan (C16), whichever is non-zero first.
            var bfLoanAmount  = ParseDecimal(Cell(sheet, r, 13));
            var firstLoan     = ParseDecimal(Cell(sheet, r, 14));
            var secondLoan    = ParseDecimal(Cell(sheet, r, 15));
            var thirdLoan     = ParseDecimal(Cell(sheet, r, 16));
            var loanAmount    = bfLoanAmount > 0 ? bfLoanAmount
                : firstLoan > 0 ? firstLoan
                : secondLoan > 0 ? secondLoan
                : thirdLoan;

            list.Add(new ExcelRow
            {
                RowNum     = r,
                JoiningDate = ParseDate(Cell(sheet, r, 3)),
                MemberCode = memberCode,
                MemberName = Cell(sheet, r, 6) ?? "",
                Age        = ParseInt(Cell(sheet, r, 7)),
                GuardianName  = Cell(sheet, r, 8) ?? "",
                GuardianAge   = ParseInt(Cell(sheet, r, 9)),
                Village    = village,
                PocName    = pocName,
                HandledBy  = handledBy,
                Phone      = phones.Length > 0 ? phones[0].Trim() : "",
                AltPhone   = phones.Length > 1 ? phones[1].Trim() : null,
                DisbDate   = ParseDate(Cell(sheet, r, 12)) ?? DateTime.UtcNow,
                LoanAmount = loanAmount,
                OutstandingAmount = ParseDecimal(Cell(sheet, r, 25)),
                InsuranceFee     = ParseDecimal(Cell(sheet, r, 21)),
                WeeklyDue  = ParseDecimal(Cell(sheet, r, 22)),
                Status     = Cell(sheet, r, 23) ?? "Active",
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
        using var cmd = (await _db.GetConn()).CreateCommand();
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

        using var ins = (await _db.GetConn()).CreateCommand();
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
        using var cmd = (await _db.GetConn()).CreateCommand();
        cmd.CommandText = "SELECT Id FROM Centers WHERE Name = @name AND BranchId = @branchId AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@name", village);
        cmd.Parameters.AddWithValue("@branchId", branchId);
        var existing = await cmd.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = (await _db.GetConn()).CreateCommand();
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

    private async Task<int> GetOrCreateStaffUserAsync(string fullName, int branchId)
    {
        var emailPart = fullName.Trim().ToLowerInvariant().Replace(' ', '.');
        var email     = $"{emailPart}.staff@{EmailDomain}";

        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@email", email);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value) return Convert.ToInt32(existing);

        var parts = fullName.Trim().Split(' ', 2);
        using var chk2 = (await _db.GetConn()).CreateCommand();
        chk2.CommandText = "SELECT TOP 1 Id FROM Users WHERE FirstName = @fn AND IsDeleted = 0 AND [Level] = 'Branch'";
        chk2.Parameters.AddWithValue("@fn", parts[0]);
        var existing2 = await chk2.ExecuteScalarAsync();
        if (existing2 != null && existing2 != DBNull.Value) return Convert.ToInt32(existing2);

        var pwdHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"INSERT INTO Users
            (FirstName, LastName, Email, PasswordHash, Role, [Level], OrgId, BranchId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, @email, @pwd, 'Staff', 'Branch', @orgId, @branchId, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn",        parts[0]);
        ins.Parameters.AddWithValue("@ln",        parts.Length > 1 ? parts[1] : "-");
        ins.Parameters.AddWithValue("@email",     email);
        ins.Parameters.AddWithValue("@pwd",       pwdHash);
        ins.Parameters.AddWithValue("@orgId",     _orgId);
        ins.Parameters.AddWithValue("@branchId",  branchId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"  [CREATED] staff '{fullName}'  email={email}  id={newId}  branchId={branchId}");
        return newId;
    }

    private async Task<int> GetOrCreatePocAsync(string staffName, int centerId, int collectionByUserId)
    {
        // Name: first word = FirstName, rest = LastName
        var parts = staffName.Trim().Split(' ', 2);
        var firstName = parts[0];
        var lastName  = parts.Length > 1 ? parts[1] : "-";

        using var cmd = (await _db.GetConn()).CreateCommand();
        cmd.CommandText = "SELECT Id FROM POCs WHERE FirstName = @fn AND LastName = @ln AND CenterId = @cid AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@fn", firstName);
        cmd.Parameters.AddWithValue("@ln", lastName);
        cmd.Parameters.AddWithValue("@cid", centerId);
        var existing = await cmd.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"
            INSERT INTO POCs (FirstName, LastName, PhoneNumber, CenterId, CollectionFrequency, CollectionBy, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, '0000000000', @cid, 'Weekly', @collectionBy, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn", firstName);
        ins.Parameters.AddWithValue("@ln", lastName);
        ins.Parameters.AddWithValue("@cid", centerId);
        ins.Parameters.AddWithValue("@collectionBy", collectionByUserId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] poc          => id={newId} '{staffName}' centerId={centerId} collectionBy={collectionByUserId}");
        return newId;
    }

    private async Task<int> GetAnyPocInCenterAsync(int centerId)
    {
        using var cmd = (await _db.GetConn()).CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id FROM POCs WHERE CenterId = @cid AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@cid", centerId);
        var r = await cmd.ExecuteScalarAsync();
        return r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
    }

    private async Task<int> GetOrCreateMemberAsync(ExcelRow row, int centerId, int pocId)
    {
        // Dedup by MemberCode first — it's the actual unique key in the sheet/DB
        if (!string.IsNullOrWhiteSpace(row.MemberCode))
        {
            using var chkCode = (await _db.GetConn()).CreateCommand();
            chkCode.CommandText = "SELECT Id FROM Members WHERE MemberCode = @code AND IsDeleted = 0";
            chkCode.Parameters.AddWithValue("@code", row.MemberCode);
            var exCode = await chkCode.ExecuteScalarAsync();
            if (exCode != null && exCode != DBNull.Value)
                return Convert.ToInt32(exCode);
        }

        // Fallback dedup by phone number (skip the shared placeholder "0000000000")
        if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone != "0000000000")
        {
            using var chk = (await _db.GetConn()).CreateCommand();
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

        using var ins = (await _db.GetConn()).CreateCommand();
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

    /// <summary>
    /// Returns (LoanId, IsNew, NoOfTerms, ProcessingFee). If a loan already exists for the
    /// member, no schedule/ledger tx is regenerated by the caller.
    /// Creates a 'Loan disbursement' LedgerTransaction: PaidFrom=branch staff, PaidTo=NULL
    /// (the branch staff funds the loan — money no longer routes through ImportUser).
    /// </summary>
    private async Task<(int LoanId, bool IsNew, int NoOfTerms, decimal ProcessingFee)> GetOrCreateLoanAsync(ExcelRow row, int memberId, int staffUserId)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id, NoOfTerms, ProcessingFee FROM Loans WHERE MemberId = @mid AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@mid", memberId);
        using (var r = await chk.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
                return (r.GetInt32(0), false, r.GetInt32(1), r.GetDecimal(2));
        }

        var processingFee = Math.Round(row.LoanAmount * ProcessingFeeRate, 2);
        var totalAmount   = row.LoanAmount + processingFee + row.InsuranceFee;
        var noOfTerms     = row.WeeklyDue > 0 ? (int)Math.Ceiling(row.LoanAmount / row.WeeklyDue) : 25;
        var status        = row.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ? "Closed" : "Active";
        var disbDate      = row.DisbDate;

        using var ins = (await _db.GetConn()).CreateCommand();
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
        ins.Parameters.AddWithValue("@processingFee", processingFee);
        ins.Parameters.AddWithValue("@insuranceFee", row.InsuranceFee);
        ins.Parameters.AddWithValue("@totalAmount", totalAmount);
        ins.Parameters.AddWithValue("@status", status);
        ins.Parameters.AddWithValue("@disbDate", disbDate);
        ins.Parameters.AddWithValue("@noOfTerms", noOfTerms);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var loanId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"    [LOAN]  id={loanId} amount={row.LoanAmount:N0} processingFee={processingFee:N0} status={status} terms={noOfTerms}");

        // Loan disbursement ledger tx: branch staff → NULL (member, no User row)
        using var tx = (await _db.GetConn()).CreateCommand();
        tx.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            VALUES (@from, NULL, @amount, @date, @createdBy, GETUTCDATE(), 'Loan disbursement', @refId, @comments)";
        tx.Parameters.AddWithValue("@from",      staffUserId);
        tx.Parameters.AddWithValue("@amount",    row.LoanAmount);
        tx.Parameters.AddWithValue("@date",      disbDate);
        tx.Parameters.AddWithValue("@createdBy", _importUserId);
        tx.Parameters.AddWithValue("@refId",     loanId);
        tx.Parameters.AddWithValue("@comments",  $"Loan disbursement for Loan ID: {loanId}, Member ID: {memberId}");
        await tx.ExecuteNonQueryAsync();

        return (loanId, true, noOfTerms, processingFee);
    }

    /// <summary>Returns true if a new membership fee + ledger tx was created.</summary>
    private async Task<bool> GetOrCreateMembershipFeeAndLedgerAsync(int memberId, DateTime paidDate, int staffUserId)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT COUNT(1) FROM MemberMembershipFees WHERE MemberId = @mid AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@mid", memberId);
        if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) return false;

        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"
            INSERT INTO MemberMembershipFees (MemberId, Amount, PaidDate, CollectedBy, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@mid, @amount, @paidDate, @collectedBy, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@mid", memberId);
        ins.Parameters.AddWithValue("@amount", MembershipFeeAmount);
        ins.Parameters.AddWithValue("@paidDate", paidDate);
        ins.Parameters.AddWithValue("@collectedBy", staffUserId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var feeId = Convert.ToInt32(await ins.ExecuteScalarAsync());

        using var tx = (await _db.GetConn()).CreateCommand();
        tx.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            VALUES (NULL, @paidTo, @amount, @date, @createdBy, GETUTCDATE(), 'Member Joining Fee', @refId, @comments)";
        tx.Parameters.AddWithValue("@paidTo",    staffUserId);
        tx.Parameters.AddWithValue("@amount",    MembershipFeeAmount);
        tx.Parameters.AddWithValue("@date",      paidDate);
        tx.Parameters.AddWithValue("@createdBy", _importUserId);
        tx.Parameters.AddWithValue("@refId",     feeId);
        tx.Parameters.AddWithValue("@comments",  $"Member joining fee for Member ID: {memberId} (fee record {feeId}).");
        await tx.ExecuteNonQueryAsync();

        return true;
    }

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @uid";
        chk.Parameters.AddWithValue("@uid", userId);
        using var r = await chk.ExecuteReaderAsync();
        bool found      = await r.ReadAsync();
        int ledgerId    = found ? r.GetInt32(0) : 0;
        decimal current = found ? r.GetDecimal(1) : 0m;
        r.Close();

        if (found)
        {
            using var upd = (await _db.GetConn()).CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", current + delta);
            upd.Parameters.AddWithValue("@id",     ledgerId);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            using var ins = (await _db.GetConn()).CreateCommand();
            ins.CommandText = "INSERT INTO Ledgers (UserId, Amount) VALUES (@uid, @amount)";
            ins.Parameters.AddWithValue("@uid",    userId);
            ins.Parameters.AddWithValue("@amount", delta);
            await ins.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Insurance_Claim_Financial_Summary is a single running-totals row (no per-user/loan
    /// rows). Mirrors AccumulateLoanCreationTotalsAsync + AccumulateJoiningFeeAsync.
    /// </summary>
    private async Task AccumulateFinancialSummaryAsync(decimal insuranceFee, decimal processingFee, decimal joiningFee)
    {
        int? summaryId = null;
        using (var chk = (await _db.GetConn()).CreateCommand())
        {
            chk.CommandText = "SELECT TOP 1 SummaryId FROM Insurance_Claim_Financial_Summary ORDER BY SummaryId";
            var existing = await chk.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
                summaryId = Convert.ToInt32(existing);
        }

        if (summaryId.HasValue)
        {
            using var upd = (await _db.GetConn()).CreateCommand();
            upd.CommandText = @"UPDATE Insurance_Claim_Financial_Summary
                SET TotalInsuranceAmount = TotalInsuranceAmount + @ins,
                    TotalProcessingFee   = TotalProcessingFee + @proc,
                    TotalJoiningFee      = TotalJoiningFee + @join
                WHERE SummaryId = @id";
            upd.Parameters.AddWithValue("@ins",  insuranceFee);
            upd.Parameters.AddWithValue("@proc", processingFee);
            upd.Parameters.AddWithValue("@join", joiningFee);
            upd.Parameters.AddWithValue("@id",   summaryId.Value);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            using var ins = (await _db.GetConn()).CreateCommand();
            ins.CommandText = @"INSERT INTO Insurance_Claim_Financial_Summary
                (TotalInsuranceAmount, TotalClaimedAmount, TotalProcessingFee, TotalJoiningFee, TotalExpenseAmount, CreatedDate)
                VALUES (@ins, 0, @proc, @join, 0, GETUTCDATE())";
            ins.Parameters.AddWithValue("@ins",  insuranceFee);
            ins.Parameters.AddWithValue("@proc", processingFee);
            ins.Parameters.AddWithValue("@join", joiningFee);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── LoanSchedulers + paid-installment ledger generation ─────────────────────

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
        dt.Columns.Add("PaymentMode",            typeof(string));
        dt.Columns.Add("CollectedBy",            typeof(int));
        dt.Columns.Add("CreatedBy",              typeof(int));
        dt.Columns.Add("CreatedDate",            typeof(DateTime));
        return dt;
    }

    private static DataTable MakeLedgerTxTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("PaidFromUserId",  typeof(int));
        dt.Columns.Add("PaidToUserId",    typeof(int));
        dt.Columns.Add("Amount",          typeof(decimal));
        dt.Columns.Add("PaymentDate",     typeof(DateTime));
        dt.Columns.Add("CreatedBy",       typeof(int));
        dt.Columns.Add("CreatedDate",     typeof(DateTime));
        dt.Columns.Add("TransactionType", typeof(string));
        dt.Columns.Add("ReferenceId",     typeof(int));
        dt.Columns.Add("Comments",        typeof(string));
        return dt;
    }

    /// <summary>
    /// Generates weekly installment rows starting 7 days after DisbDate (collection day =
    /// same weekday as disbursement), for NoOfTerms installments.
    ///
    /// weeksOutstanding = ceil(OutstandingAmount("Out" column) / WeeklyDue);
    /// paidWeeks = NoOfTerms - weeksOutstanding.
    /// If Status="Closed", the whole loan is treated as fully paid (paidWeeks = NoOfTerms).
    ///
    /// For i &lt;= paidWeeks: scheduler Status='Paid' + one ledger tx
    ///   (NULL→Staff type='EMI Recovery'). Money stays with staff — no further hop.
    /// For i &gt; paidWeeks: scheduler Status='NotPaid', no ledger tx.
    /// </summary>
    private void QueueSchedulersAndPayments(
        DataTable schedulerTable, DataTable ledgerTxTable, Dictionary<int, decimal> ledgerDeltas,
        int loanId, int memberId, DateTime disbDate, decimal loanAmount, decimal weeklyDue,
        int noOfTerms, decimal outstandingAmount, string status, int staffUserId)
    {
        if (noOfTerms <= 0) return;

        var isClosed = status.Equals("Closed", StringComparison.OrdinalIgnoreCase);
        var weeksOutstanding = isClosed ? 0
            : (weeklyDue > 0 ? (int)Math.Ceiling(outstandingAmount / weeklyDue) : noOfTerms);
        var paidWeeks = isClosed ? noOfTerms : Math.Clamp(noOfTerms - weeksOutstanding, 0, noOfTerms);

        var installmentAmt = weeklyDue > 0 ? weeklyDue : Math.Round(loanAmount / noOfTerms, 2);
        decimal remaining = loanAmount;
        var scheduleDate = disbDate;

        for (int i = 1; i <= noOfTerms; i++)
        {
            scheduleDate = scheduleDate.AddDays(7);
            var amount = i == noOfTerms ? remaining : Math.Min(installmentAmt, remaining);
            remaining -= amount;

            var isPaid = i <= paidWeeks;

            var row = schedulerTable.NewRow();
            row["LoanId"]                = loanId;
            row["ScheduleDate"]          = scheduleDate;
            row["PaymentDate"]           = isPaid ? scheduleDate : (object)DBNull.Value;
            // ActualEmiAmount always equals the scheduled amount (Paid or Not Paid).
            // PaymentAmount ("Paid Amount") is 0 when Not Paid, the amount when Paid.
            // ActualPrincipalAmount/ActualInterestAmount stay 0 until actually paid.
            row["ActualEmiAmount"]       = amount;
            row["ActualPrincipalAmount"] = isPaid ? amount : 0m;
            row["ActualInterestAmount"]  = 0m;
            row["PaymentAmount"]         = isPaid ? amount : 0m;
            row["PrincipalAmount"]       = amount;
            row["InterestAmount"]        = 0m;
            row["InstallmentNo"]         = i;
            row["Status"]                = isPaid ? "Paid" : "Not Paid";
            row["PaymentMode"]           = isPaid ? "Cash" : (object)DBNull.Value;
            row["CollectedBy"]           = staffUserId;
            row["CreatedBy"]             = _importUserId;
            row["CreatedDate"]           = DateTime.UtcNow;
            schedulerTable.Rows.Add(row);

            if (!isPaid) continue;

            // Member (NULL) → Staff : 'EMI Recovery'
            var tx1 = ledgerTxTable.NewRow();
            tx1["PaidFromUserId"]  = DBNull.Value;
            tx1["PaidToUserId"]    = staffUserId;
            tx1["Amount"]          = amount;
            tx1["PaymentDate"]     = scheduleDate;
            tx1["CreatedBy"]       = _importUserId;
            tx1["CreatedDate"]     = DateTime.UtcNow;
            tx1["TransactionType"] = "EMI Recovery";
            tx1["ReferenceId"]     = loanId;
            tx1["Comments"]        = $"EMI recovery posted for Loan ID: {loanId}, Member ID: {memberId}, installment #{i}.";
            ledgerTxTable.Rows.Add(tx1);

            // Money stays with staff — no further hop to ImportUser.
            ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) + amount;
        }
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
    public string? PocName    { get; set; }
    public string Phone       { get; set; } = "";
    public string? AltPhone   { get; set; }
    public DateTime DisbDate  { get; set; }
    public decimal LoanAmount { get; set; }
    /// <summary>"Out" column (C25) — outstanding amount; weeksOutstanding = OutstandingAmount ÷ WeeklyDue.</summary>
    public decimal OutstandingAmount { get; set; }
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
