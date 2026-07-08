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
    // Matches the seeded PaymentTerm "30Week-ROI-24" and the web UI's AddLoanDialog formula:
    // InterestAmount = LoanAmount × RateOfInterest/100, ProcessingFee = LoanAmount × ProcessingFee%/100.
    private const decimal RateOfInterestPct   = 20.00m;
    private const decimal ProcessingFeePct    = 2.25m;
    private const int     FixedNoOfTerms      = 30;
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
        var sheet = GetMasterGroupSheet(pkg);

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
        var sheet = GetMasterGroupSheet(pkg);

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
                pocMap[key] = await GetOrCreatePocAsync(row.PocName, centerId, staffUserId, row.CollectionDay);
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

                // One or two loans per member (see ExcelRow.Loans doc comment).
                foreach (var loanInfo in row.Loans)
                {
                    var (loanId, isNew, noOfTerms, interestAmount) =
                        await GetOrCreateLoanAsync(loanInfo, row, memberId, staffUserId);

                    if (isNew)
                    {
                        // Branch staff funds the loan — money flows through staff, not ImportUser.
                        ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) - loanInfo.Amount;
                        // Fees are NOT written to LedgerTransactions — only accumulated in
                        // Insurance_Claim_Financial_Summary (matches LoansService convention).
                        var processingFee = Math.Round(loanInfo.Amount * ProcessingFeePct / 100m, 2);
                        totalInsuranceFee  += row.InsuranceFee;
                        totalProcessingFee += processingFee;

                        // isClosed (forced for the historical B/F loan) always fully pays it off,
                        // regardless of the row's OutstandingAmount/WeeksOutstanding (those apply
                        // only to the currently-active loan).
                        var scheduleDates = QueueSchedulersAndPayments(schedulerTable, ledgerTxTable, ledgerDeltas,
                            loanId, memberId, loanInfo.DisbDate, loanInfo.Amount, interestAmount,
                            noOfTerms, row.OutstandingAmount, row.WeeksOutstandingDirect, loanInfo.Status,
                            staffUserId, row.CollectionDay, row.EmiStartDate);

                        if (scheduleDates.HasValue)
                        {
                            // DisbursementDate and CollectionStartDate must both equal the
                            // first LoanScheduler's ScheduleDate.
                            var closureDate = loanInfo.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                                ? scheduleDates.Value.LastPaidScheduleDate
                                : null;
                            await UpdateLoanDatesAsync(loanId, scheduleDates.Value.FirstScheduleDate, closureDate);
                        }
                    }
                }

                // Fixed membership fee + ledger tx (NULL → Handled-By staff)
                var joinDate = row.JoiningDate ?? row.Loans.FirstOrDefault()?.DisbDate ?? DateTime.UtcNow.Date;
                var feeCreated = await GetOrCreateMembershipFeeAndLedgerAsync(memberId, joinDate, staffUserId);
                if (feeCreated)
                {
                    ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) + MembershipFeeAmount;
                    totalJoiningFee += MembershipFeeAmount;
                }

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
        // Dynamic column lookups — done once outside the row loop.
        var aadharCol    = FindColumnByHeader(sheet, 5, "Member Aadhar", "Member Aadhaar", "Aadhar", "Aadhaar");
        var emiStartCol  = FindColumnByHeader(sheet, 5, "EMI Start Date", "EMI Start date", "Emi Start Date");

        var list = new List<ExcelRow>();
        for (int r = 6; r <= sheet.Dimension.Rows; r++)
        {
            var memberCode = Cell(sheet, r, 5);
            if (string.IsNullOrWhiteSpace(memberCode)) continue;

            var aadhaar = aadharCol.HasValue ? Cell(sheet, r, aadharCol.Value) : null;

            // Split phone — cell sometimes has two numbers separated by newline
            var phones = Cell(sheet, r, 11)?.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            // C10 ("Village") format: "CenterName-POCName"  (first part = Center, second part = POC)
            var villageParts = (Cell(sheet, r, 10) ?? "").Split('-', 2);
            var village = villageParts[0].Trim();
            var pocName = villageParts.Length > 1 ? villageParts[1].Trim() : "--";

            // C24 = "Handled By" — the Staff name
            var handledBy = Cell(sheet, r, 24) ?? "--";

            // Loan amounts: "B/F Loan Amount" (C13), "1st Loan" (C14), "2nd Loan" (C15), "3rd Loan" (C16).
            var bfLoanAmount  = ParseDecimal(Cell(sheet, r, 13));
            var firstLoan     = ParseDecimal(Cell(sheet, r, 14));
            var secondLoan    = ParseDecimal(Cell(sheet, r, 15));
            var thirdLoan     = ParseDecimal(Cell(sheet, r, 16));

            // "Disb date" (C12) may have two stacked lines (like the phone column) when a
            // member has two loans: line 1 = B/F loan's disb date, line 2 = 1st Loan's.
            var disbDateLines = (Cell(sheet, r, 12) ?? "")
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => ParseDate(d.Trim()))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();
            // .Date (no time-of-day) — matches the precision of properly-parsed Excel dates
            // (always midnight) and avoids a SQL rounding mismatch: AddWithValue infers
            // SqlDbType.DateTime (legacy ~3.33ms precision) for sub-millisecond DateTime
            // values even when the destination column is datetime2, while SqlBulkCopy
            // preserves full precision — causing Loans.DisbursementDate (set via UPDATE)
            // to silently differ from LoanSchedulers.ScheduleDate (set via bulk copy).
            var firstDisbDate  = disbDateLines.Count > 0 ? disbDateLines[0] : DateTime.UtcNow.Date;
            var secondDisbDate = disbDateLines.Count > 1 ? disbDateLines[1] : firstDisbDate;

            var statusText = Cell(sheet, r, 23) ?? "Active";

            // Loan rule:
            //  B/F>0 & 1st Loan>0 → TWO loans: B/F (historical, forced Closed) + 1st Loan
            //    (the currently active loan — uses this row's Status/Outstanding columns).
            //  B/F>0 only        → ONE loan (B/F amount), uses this row's Status.
            //  B/F==0, 1st>0     → ONE loan (1st Loan amount), uses this row's Status.
            //  Neither           → fall back to 2nd Loan → 3rd Loan (ONE loan).
            var loans = new List<LoanInfo>();
            if (bfLoanAmount > 0 && firstLoan > 0)
            {
                loans.Add(new LoanInfo { Amount = bfLoanAmount, DisbDate = firstDisbDate, Status = "Closed" });
                loans.Add(new LoanInfo { Amount = firstLoan, DisbDate = secondDisbDate, Status = statusText });
            }
            else if (bfLoanAmount > 0)
            {
                loans.Add(new LoanInfo { Amount = bfLoanAmount, DisbDate = firstDisbDate, Status = statusText });
            }
            else if (firstLoan > 0)
            {
                loans.Add(new LoanInfo { Amount = firstLoan, DisbDate = firstDisbDate, Status = statusText });
            }
            else if (secondLoan > 0)
            {
                loans.Add(new LoanInfo { Amount = secondLoan, DisbDate = firstDisbDate, Status = statusText });
            }
            else if (thirdLoan > 0)
            {
                loans.Add(new LoanInfo { Amount = thirdLoan, DisbDate = firstDisbDate, Status = statusText });
            }

            // C27 = "No.of weeks Outstanding" — may be "#DIV/0!" for Closed rows
            // (WeeklyDue=0). Parse if numeric, else fall back to Out(C25)/WeeklyDue later.
            var weeksOutstandingText = Cell(sheet, r, 27);
            int? weeksOutstandingDirect = int.TryParse(weeksOutstandingText, out var wo) ? wo : (int?)null;

            DateTime? emiStartDate = emiStartCol.HasValue ? ParseDate(Cell(sheet, r, emiStartCol.Value)) : null;

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
                Aadhaar    = aadhaar,
                Loans      = loans,
                OutstandingAmount = ParseDecimal(Cell(sheet, r, 25)),
                InsuranceFee     = ParseDecimal(Cell(sheet, r, 21)),
                WeeklyDue  = ParseDecimal(Cell(sheet, r, 22)),
                Status     = statusText,
                CollectionDay      = Cell(sheet, r, 26),
                WeeksOutstandingDirect = weeksOutstandingDirect,
                EmiStartDate = emiStartDate,
            });
        }
        return list;
    }

    /// <summary>Tries the corrected "Master Group" spelling first, falls back to the old "Master Gruop" typo.</summary>
    private static ExcelWorksheet GetMasterGroupSheet(ExcelPackage pkg)
    {
        var sheet = pkg.Workbook.Worksheets["Master Group"] ?? pkg.Workbook.Worksheets["Master Gruop"];
        if (sheet?.Dimension == null)
            throw new InvalidOperationException("Sheet 'Master Group' (or 'Master Gruop') not found or is empty.");
        return sheet;
    }

    private static string? Cell(ExcelWorksheet s, int r, int c) =>
        s.Cells[r, c].Text?.Trim().NullIfEmpty();

    /// <summary>Finds a column by matching any of the candidate header texts (case-insensitive) in headerRow.</summary>
    private static int? FindColumnByHeader(ExcelWorksheet sheet, int headerRow, params string[] candidates)
    {
        for (int c = 1; c <= sheet.Dimension.Columns; c++)
        {
            var header = sheet.Cells[headerRow, c].Text?.Trim();
            if (string.IsNullOrWhiteSpace(header)) continue;
            foreach (var candidate in candidates)
                if (string.Equals(header, candidate, StringComparison.OrdinalIgnoreCase))
                    return c;
        }
        return null;
    }

    /// <summary>
    /// Shifts <paramref name="date"/> by the smallest +/- offset (at most 3 days either
    /// way) so it lands exactly on the weekday named by <paramref name="collectionDay"/>
    /// (e.g. "Monday", "Tuesday"). If collectionDay is missing/unparseable, returns the
    /// date unchanged.
    /// </summary>
    private static DateTime AlignToCollectionDay(DateTime date, string? collectionDay)
    {
        if (string.IsNullOrWhiteSpace(collectionDay)) return date;
        if (!Enum.TryParse<DayOfWeek>(collectionDay.Trim(), true, out var targetDay)) return date;

        var diff = ((int)targetDay - (int)date.DayOfWeek + 7) % 7;
        if (diff > 3) diff -= 7; // shift the other way if that's the nearer direction
        return date.AddDays(diff);
    }

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

        var parts = fullName.Trim().Split(' ', 2);

        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id FROM Users WHERE Email = @email AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@email", email);
        var existing = await chk.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
        {
            var existingId = Convert.ToInt32(existing);
            // BranchId may have been nulled by a previous cleanup — restore it.
            using var fix = (await _db.GetConn()).CreateCommand();
            fix.CommandText = "UPDATE Users SET BranchId = @branchId WHERE Id = @id AND (BranchId IS NULL OR BranchId <> @branchId)";
            fix.Parameters.AddWithValue("@branchId", branchId);
            fix.Parameters.AddWithValue("@id", existingId);
            var updated = await fix.ExecuteNonQueryAsync();
            if (updated > 0)
                Console.WriteLine($"  [UPDATED] staff '{fullName}' id={existingId} BranchId restored to {branchId}");
            return existingId;
        }

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

    private async Task<int> GetOrCreatePocAsync(string staffName, int centerId, int collectionByUserId, string? collectionDay)
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
        {
            var existingId = Convert.ToInt32(existing);
            if (!string.IsNullOrWhiteSpace(collectionDay))
            {
                using var upd = (await _db.GetConn()).CreateCommand();
                upd.CommandText = "UPDATE POCs SET CollectionDay = @day WHERE Id = @id AND (CollectionDay IS NULL OR CollectionDay <> @day)";
                upd.Parameters.AddWithValue("@day", collectionDay);
                upd.Parameters.AddWithValue("@id", existingId);
                var rows = await upd.ExecuteNonQueryAsync();
                if (rows > 0)
                    Console.WriteLine($"[UPDATED] poc          => id={existingId} CollectionDay='{collectionDay}'");
            }
            return existingId;
        }

        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"
            INSERT INTO POCs (FirstName, LastName, PhoneNumber, CenterId, CollectionFrequency, CollectionDay, CollectionBy, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@fn, @ln, '0000000000', @cid, 'Weekly', @collectionDay, @collectionBy, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn", firstName);
        ins.Parameters.AddWithValue("@ln", lastName);
        ins.Parameters.AddWithValue("@cid", centerId);
        ins.Parameters.AddWithValue("@collectionDay", (object?)collectionDay ?? DBNull.Value);
        ins.Parameters.AddWithValue("@collectionBy", collectionByUserId);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);
        var newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"[CREATED] poc          => id={newId} '{staffName}' centerId={centerId} collectionBy={collectionByUserId} collectionDay={collectionDay}");
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
        // Dedup by MemberCode only — it's the actual unique key in the sheet/DB.
        // Phone numbers are NOT a reliable dedup key here: group-lending members often
        // share one household/group contact number across genuinely different people
        // (confirmed: 25 phone numbers in this sheet are each shared by two distinct,
        // differently-named members with their own separate loans).
        if (!string.IsNullOrWhiteSpace(row.MemberCode))
        {
            using var chkCode = (await _db.GetConn()).CreateCommand();
            chkCode.CommandText = "SELECT Id FROM Members WHERE MemberCode = @code AND IsDeleted = 0";
            chkCode.Parameters.AddWithValue("@code", row.MemberCode);
            var exCode = await chkCode.ExecuteScalarAsync();
            if (exCode != null && exCode != DBNull.Value)
                return Convert.ToInt32(exCode);
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
                (FirstName, LastName, PhoneNumber, AltPhone, Address1, MemberCode, Aadhaar,
                 Age, GuardianFirstName, GuardianLastName, GuardianPhone, GuardianAge,
                 CenterId, POCId, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES
                (@fn, @ln, @phone, @altPhone, @village, @memberCode, @aadhaar,
                 @age, @gfn, @gln, @gphone, @gage,
                 @centerId, @pocId, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@fn", firstName);
        ins.Parameters.AddWithValue("@ln", lastName);
        ins.Parameters.AddWithValue("@phone", phone);
        ins.Parameters.AddWithValue("@altPhone", (object?)altPhone ?? DBNull.Value);
        ins.Parameters.AddWithValue("@village", row.Village);
        ins.Parameters.AddWithValue("@memberCode", (object?)row.MemberCode.NullIfEmpty() ?? DBNull.Value);
        // Aadhaar has a unique index — blank stays NULL (SQL Server allows multiple NULLs).
        ins.Parameters.AddWithValue("@aadhaar", (object?)row.Aadhaar.NullIfEmpty() ?? DBNull.Value);
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
    /// Returns (LoanId, IsNew, NoOfTerms, InterestAmount). A member can have up to two
    /// loans (one historical/Closed, one currently active) — dedup is by
    /// (MemberId, DisbursementDate), not MemberId alone, so the second loan still gets
    /// created. The DB only enforces uniqueness on simultaneously-OPEN loans per member,
    /// which this satisfies (at most one Active loan per member at a time).
    /// InterestAmount/ProcessingFee/NoOfTerms match the seeded PaymentTerm "30Week-ROI-24"
    /// and the web UI's AddLoanDialog formula (LoanAmount × rate/100).
    /// Creates a 'Loan disbursement' LedgerTransaction: PaidFrom=branch staff, PaidTo=NULL
    /// (the branch staff funds the loan — money no longer routes through ImportUser).
    /// </summary>
    private async Task<(int LoanId, bool IsNew, int NoOfTerms, decimal InterestAmount)> GetOrCreateLoanAsync(
        LoanInfo loanInfo, ExcelRow row, int memberId, int staffUserId)
    {
        using var chk = (await _db.GetConn()).CreateCommand();
        chk.CommandText = "SELECT Id, NoOfTerms, InterestAmount FROM Loans WHERE MemberId = @mid AND DisbursementDate = @disbDate AND IsDeleted = 0";
        chk.Parameters.AddWithValue("@mid", memberId);
        chk.Parameters.AddWithValue("@disbDate", loanInfo.DisbDate);
        using (var r = await chk.ExecuteReaderAsync())
        {
            if (await r.ReadAsync())
                return (r.GetInt32(0), false, r.GetInt32(1), r.GetDecimal(2));
        }

        var interestAmount = Math.Round(loanInfo.Amount * RateOfInterestPct / 100m, 2);
        var processingFee  = Math.Round(loanInfo.Amount * ProcessingFeePct / 100m, 2);
        var totalAmount    = Math.Round(loanInfo.Amount + interestAmount, 2);
        var noOfTerms      = FixedNoOfTerms;
        var status         = loanInfo.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ? "Closed" : "Active";
        var disbDate       = loanInfo.DisbDate;

        using var ins = (await _db.GetConn()).CreateCommand();
        ins.CommandText = @"
            INSERT INTO Loans
                (MemberId, LoanAmount, InterestAmount, ProcessingFee, InsuranceFee,
                 IsSavingEnabled, SavingAmount, TotalAmount, Status,
                 DisbursementDate, CollectionStartDate, CollectionTerm, NoOfTerms,
                 CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES
                (@mid, @loanAmount, @interestAmount, @processingFee, @insuranceFee,
                 0, 0, @totalAmount, @status,
                 @disbDate, @disbDate, 'Weekly', @noOfTerms,
                 @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@mid", memberId);
        ins.Parameters.AddWithValue("@loanAmount", loanInfo.Amount);
        ins.Parameters.AddWithValue("@interestAmount", interestAmount);
        ins.Parameters.AddWithValue("@processingFee", processingFee);
        ins.Parameters.AddWithValue("@insuranceFee", row.InsuranceFee);
        ins.Parameters.AddWithValue("@totalAmount", totalAmount);
        ins.Parameters.AddWithValue("@status", status);
        ins.Parameters.AddWithValue("@disbDate", disbDate);
        ins.Parameters.AddWithValue("@noOfTerms", noOfTerms);
        ins.Parameters.AddWithValue("@createdBy", _importUserId);

        var loanId = Convert.ToInt32(await ins.ExecuteScalarAsync());
        Console.WriteLine($"    [LOAN]  id={loanId} amount={loanInfo.Amount:N0} interest={interestAmount:N0} processingFee={processingFee:N0} status={status} terms={noOfTerms} disb={disbDate:yyyy-MM-dd}");

        // Loan disbursement ledger tx: branch staff → NULL (member, no User row)
        using var tx = (await _db.GetConn()).CreateCommand();
        tx.CommandText = @"
            INSERT INTO LedgerTransactions
                (PaidFromUserId, PaidToUserId, Amount, PaymentDate, CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
            VALUES (@from, NULL, @amount, @date, @createdBy, GETUTCDATE(), 'Loan disbursement', @refId, @comments)";
        tx.Parameters.AddWithValue("@from",      staffUserId);
        tx.Parameters.AddWithValue("@amount",    loanInfo.Amount);
        tx.Parameters.AddWithValue("@date",      disbDate);
        tx.Parameters.AddWithValue("@createdBy", _importUserId);
        tx.Parameters.AddWithValue("@refId",     loanId);
        tx.Parameters.AddWithValue("@comments",  $"Loan disbursement for Loan ID: {loanId}, Member ID: {memberId}");
        await tx.ExecuteNonQueryAsync();

        return (loanId, true, noOfTerms, interestAmount);
    }

    /// <summary>
    /// Sets DisbursementDate AND CollectionStartDate to the first LoanScheduler's
    /// ScheduleDate (both must be the same value), and ClosureDate to the date of the
    /// loan's last (final) payment when provided (Closed loans only).
    /// </summary>
    private async Task UpdateLoanDatesAsync(int loanId, DateTime firstScheduleDate, DateTime? closureDate)
    {
        using var upd = (await _db.GetConn()).CreateCommand();
        upd.CommandText = @"UPDATE Loans
            SET DisbursementDate = @firstDate, CollectionStartDate = @firstDate, ClosureDate = @closureDate
            WHERE Id = @id";
        upd.Parameters.AddWithValue("@firstDate", firstScheduleDate);
        upd.Parameters.AddWithValue("@closureDate", (object?)closureDate ?? DBNull.Value);
        upd.Parameters.AddWithValue("@id", loanId);
        await upd.ExecuteNonQueryAsync();
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
            INSERT INTO MemberMembershipFees (MemberId, Amount, PaidDate, CollectedBy, PaymentMode, CreatedBy, CreatedAt, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@mid, @amount, @paidDate, @collectedBy, @paymentMode, @createdBy, GETUTCDATE(), 0)";
        ins.Parameters.AddWithValue("@mid", memberId);
        ins.Parameters.AddWithValue("@amount", MembershipFeeAmount);
        ins.Parameters.AddWithValue("@paidDate", paidDate);
        ins.Parameters.AddWithValue("@collectedBy", staffUserId);
        ins.Parameters.AddWithValue("@paymentMode", "Cash");
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
    /// Principal/interest split per installment matches the live app's
    /// LoanSchedulerService.GenerateEMIScheduleAsync exactly:
    ///   principalPerInstallment = LoanAmount / NoOfTerms
    ///   interestPerInstallment  = InterestAmount / NoOfTerms
    /// with the last installment absorbing the rounding remainder.
    ///
    /// weeksOutstanding = the "No.of weeks Outstanding" column (C27) when it's a parseable
    /// number; falls back to ceil(OutstandingAmount("Out" column) / WeeklyDue) when that
    /// column is "#DIV/0!" or missing. paidWeeks = NoOfTerms - weeksOutstanding.
    /// If Status="Closed", the whole loan is treated as fully paid (paidWeeks = NoOfTerms).
    ///
    /// For i &lt;= paidWeeks: scheduler Status='Paid'. PrincipalAmount/InterestAmount for the
    /// payment are derived via CalculatePrepaymentSplit — the same ratio-based split as the
    /// web UI's calculatePrepaymentSplit() — plus one ledger tx (NULL→Staff type='EMI Recovery').
    /// Money stays with staff — no further hop.
    /// For i &gt; paidWeeks: scheduler Status='NotPaid', no ledger tx.
    /// </summary>
    /// <summary>
    /// Returns the first installment's schedule date (used to set Loans.DisbursementDate
    /// AND Loans.CollectionStartDate — both must equal it) and the schedule date of the
    /// last Paid installment (used to set Loans.ClosureDate for Closed loans).
    /// </summary>
    private (DateTime FirstScheduleDate, DateTime? LastPaidScheduleDate)? QueueSchedulersAndPayments(
        DataTable schedulerTable, DataTable ledgerTxTable, Dictionary<int, decimal> ledgerDeltas,
        int loanId, int memberId, DateTime disbDate, decimal loanAmount, decimal interestAmount,
        int noOfTerms, decimal outstandingAmount, int? weeksOutstandingDirect, string status,
        int staffUserId, string? collectionDay, DateTime? emiStartDate = null)
    {
        if (noOfTerms <= 0) return null;

        var isClosed = status.Equals("Closed", StringComparison.OrdinalIgnoreCase);
        var weeklyDueForFallback = noOfTerms > 0 ? loanAmount / noOfTerms : 0m;
        var weeksOutstanding = isClosed ? 0
            : weeksOutstandingDirect
              ?? (weeklyDueForFallback > 0 ? (int)Math.Ceiling(outstandingAmount / weeklyDueForFallback) : noOfTerms);
        var paidWeeks = isClosed ? noOfTerms : Math.Clamp(noOfTerms - weeksOutstanding, 0, noOfTerms);

        // Per-installment scheduled principal/interest split (GenerateEMIScheduleAsync formula),
        // with the last installment absorbing the rounding remainder.
        var principalPerInstallment = loanAmount / noOfTerms;
        var interestPerInstallment  = interestAmount / noOfTerms;
        var scheduledPrincipal = new decimal[noOfTerms + 1];
        var scheduledInterest  = new decimal[noOfTerms + 1];
        decimal sumPrincipal = 0m, sumInterest = 0m;
        for (int i = 1; i <= noOfTerms; i++)
        {
            scheduledPrincipal[i] = Math.Round(principalPerInstallment, 2);
            scheduledInterest[i]  = Math.Round(interestPerInstallment, 2);
            sumPrincipal += scheduledPrincipal[i];
            sumInterest  += scheduledInterest[i];
        }
        scheduledPrincipal[noOfTerms] += loanAmount - sumPrincipal;
        scheduledInterest[noOfTerms]  += interestAmount - sumInterest;

        // First installment date: use the explicit "EMI Start Date" from Excel when provided;
        // otherwise fall back to DisbDate + 7 days aligned to the Collection Day weekday.
        var firstScheduleDate = emiStartDate.HasValue
            ? emiStartDate.Value.Date
            : AlignToCollectionDay(disbDate.AddDays(7), collectionDay);
        DateTime? lastPaidScheduleDate = null;

        for (int i = 1; i <= noOfTerms; i++)
        {
            var scheduleDate = firstScheduleDate.AddDays(7 * (i - 1));

            var actualPrincipal = scheduledPrincipal[i];
            var actualInterest  = scheduledInterest[i];
            var actualEmi       = actualPrincipal + actualInterest;

            var isPaid = i <= paidWeeks;

            // Matches the live app's LoanSchedulerService/RecoveryPostingRepository semantics:
            // Actual* fields ALWAYS hold the scheduled principal/interest split (set at
            // creation, unaffected by paid status). PaymentAmount/PrincipalAmount/InterestAmount
            // stay 0 until paid, then mirror the Actual* values via CalculatePrepaymentSplit —
            // the same ratio-based split as the web UI's calculatePrepaymentSplit().
            decimal paymentAmount = 0m, principalPaid = 0m, interestPaid = 0m;
            if (isPaid)
            {
                var split = CalculatePrepaymentSplit(actualEmi, actualPrincipal, actualInterest, actualEmi);
                paymentAmount = actualEmi;
                principalPaid = split.PrincipalAmount;
                interestPaid  = split.InterestAmount;
            }

            var row = schedulerTable.NewRow();
            row["LoanId"]                = loanId;
            row["ScheduleDate"]          = scheduleDate;
            row["PaymentDate"]           = isPaid ? scheduleDate : (object)DBNull.Value;
            row["ActualEmiAmount"]       = actualEmi;
            row["ActualPrincipalAmount"] = actualPrincipal;
            row["ActualInterestAmount"]  = actualInterest;
            row["PaymentAmount"]         = paymentAmount;
            row["PrincipalAmount"]       = principalPaid;
            row["InterestAmount"]        = interestPaid;
            row["InstallmentNo"]         = i;
            row["Status"]                = isPaid ? "Paid" : "Not Paid";
            row["PaymentMode"]           = isPaid ? "Cash" : (object)DBNull.Value;
            row["CollectedBy"]           = staffUserId;
            row["CreatedBy"]             = _importUserId;
            row["CreatedDate"]           = DateTime.UtcNow;
            schedulerTable.Rows.Add(row);

            if (!isPaid) continue;

            lastPaidScheduleDate = scheduleDate;

            // Member (NULL) → Staff : 'EMI Recovery'
            var tx1 = ledgerTxTable.NewRow();
            tx1["PaidFromUserId"]  = DBNull.Value;
            tx1["PaidToUserId"]    = staffUserId;
            tx1["Amount"]          = paymentAmount;
            tx1["PaymentDate"]     = scheduleDate;
            tx1["CreatedBy"]       = _importUserId;
            tx1["CreatedDate"]     = DateTime.UtcNow;
            tx1["TransactionType"] = "EMI Recovery";
            tx1["ReferenceId"]     = loanId;
            tx1["Comments"]        = $"EMI recovery posted for Loan ID: {loanId}, Member ID: {memberId}, installment #{i}.";
            ledgerTxTable.Rows.Add(tx1);

            // Money stays with staff — no further hop to ImportUser.
            ledgerDeltas[staffUserId] = ledgerDeltas.GetValueOrDefault(staffUserId) + paymentAmount;
        }

        return (firstScheduleDate, lastPaidScheduleDate);
    }

    /// <summary>
    /// Mirrors the web UI's calculatePrepaymentSplit() (MicroCredit.Web/src/pages/loan/prepaymentCalculations.ts):
    /// splits a payment into principal/interest proportionally to the scheduled
    /// ActualPrincipalAmount/ActualInterestAmount ratio.
    /// </summary>
    private static (decimal PrincipalAmount, decimal InterestAmount) CalculatePrepaymentSplit(
        decimal actualEmiAmount, decimal actualPrincipalAmount, decimal actualInterestAmount, decimal payment)
    {
        if (payment <= 0) return (0m, 0m);

        var totalFromPI = actualPrincipalAmount + actualInterestAmount;
        if (totalFromPI > 0)
        {
            var principalRatio = actualPrincipalAmount / totalFromPI;
            var principalAmount = Math.Round(payment * principalRatio, 2);
            return (principalAmount, Math.Round(payment - principalAmount, 2));
        }

        if (actualEmiAmount > 0)
        {
            var principalRatio = actualPrincipalAmount / actualEmiAmount;
            var principalAmount = Math.Round(payment * principalRatio, 2);
            return (principalAmount, Math.Round(payment - principalAmount, 2));
        }

        return (0m, payment);
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
    /// <summary>"Member Aadhar" column (looked up by header text, not a fixed index).</summary>
    public string? Aadhaar    { get; set; }
    /// <summary>
    /// One or two loans for this member, derived from "B/F Loan Amount" / "1st Loan" per
    /// the rule: B/F&gt;0 only → one loan (B/F). 1st Loan&gt;0 only → one loan (1st Loan).
    /// Both &gt;0 → two loans: the B/F one is historical and forced Closed; the 1st Loan one
    /// is the currently active loan (uses this row's Status/OutstandingAmount/WeeksOutstanding).
    /// Each loan's own line in the (possibly two-line) "Disb date" cell is its DisbDate.
    /// </summary>
    public List<LoanInfo> Loans { get; set; } = new();
    /// <summary>"Out" column (C25) — outstanding amount; weeksOutstanding = OutstandingAmount ÷ WeeklyDue.</summary>
    public decimal OutstandingAmount { get; set; }
    public decimal InsuranceFee   { get; set; }
    public decimal WeeklyDue  { get; set; }
    public string Status      { get; set; } = "Active";
    public string HandledBy   { get; set; } = "--";
    /// <summary>"Collection Day" column (C26) — e.g. "Monday", "Tuesday".</summary>
    public string? CollectionDay { get; set; }
    /// <summary>"No.of weeks Outstanding" column (C27), when numeric (it's "#DIV/0!" for Closed rows).</summary>
    public int? WeeksOutstandingDirect { get; set; }
    /// <summary>"EMI Start Date" column — when present, used directly as first LoanScheduler date.</summary>
    public DateTime? EmiStartDate { get; set; }
}

public class LoanInfo
{
    public decimal Amount  { get; set; }
    public DateTime DisbDate { get; set; }
    public string Status   { get; set; } = "Active";
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
