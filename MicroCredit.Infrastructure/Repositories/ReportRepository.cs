using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Report;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Drawing;
using MicroCredit.Domain.Model.Fund;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly MicroCreditDbContext _context;

    public ReportRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId)
    {
        return await _context.Members
            .Where(m => !m.IsDeleted
                        && !m.POC.IsDeleted
                        && m.POC.Center.BranchId == branchId)
            .GroupBy(m => new
            {
                PocId = m.POC.Id,
                m.POC.FirstName,
                m.POC.MiddleName,
                m.POC.LastName,
                CenterName = m.POC.Center.Name
            })
            .Select(g => new ReportPocCenterResponseDto
            {
                PocId = g.Key.PocId,
                PocFullName = (
                    (g.Key.FirstName ?? string.Empty) + " " +
                    (g.Key.MiddleName ?? string.Empty) + " " +
                    (g.Key.LastName ?? string.Empty)
                ).Trim(),
                CenterName = g.Key.CenterName
            })
            .ToListAsync();
    }

    /// <summary>
    /// Members under a POC with loan schedules due today or tomorrow (calendar dates, server local),
    /// matching: Members → POCs → Centers → Branch, LEFT Loans → LEFT LoanSchedulers, DISTINCT.
    /// </summary>
    public async Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId)
    {
        // Aligns with SQL: CAST(ls.ScheduleDate AS DATE) IN (CAST(GETDATE() AS DATE), DATEADD(DAY, 1, CAST(GETDATE() AS DATE)))
        var windowStart = DateTime.Today;
        var windowEndExclusive = DateTime.Today.AddDays(2);

        var query =
            from m in _context.Members
            join p in _context.POCs on m.POCId equals p.Id
            join c in _context.Centers on p.CenterId equals c.Id
            join l in _context.Loans on m.Id equals l.MemberId into loans
            from l in loans.DefaultIfEmpty()
            join ls in _context.LoanSchedulers on l.Id equals ls.LoanId into loanSchedulers
            from ls in loanSchedulers.DefaultIfEmpty()
            where !m.IsDeleted
                  && !p.IsDeleted
                  && c.BranchId == branchId
                  && p.Id == pocId
                  && ls != null
                  && ls.Status != null
                  && ls.Status.ToLower() == "not paid"
                  && ls.ScheduleDate >= windowStart
                  && ls.ScheduleDate < windowEndExclusive
            select new ReportMembersByPocResponseDto
            {
                PocId = p.Id,
                MemberId = m.Id,
                MembersFullName = ((m.FirstName ?? string.Empty) + " " +
                                   (m.MiddleName ?? string.Empty) + " " +
                                   (m.LastName ?? string.Empty)).Trim(),
                ActualEmiAmount = ls.ActualEmiAmount,
                ScheduleDate = ls.ScheduleDate,
            };

        return await query
            .Distinct()
            .OrderBy(x => x.PocId)
            .ThenBy(x => x.MemberId)
            .ThenBy(x => x.ScheduleDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdsAsync(int branchId, IReadOnlyList<int> pocIds)
    {
        if (pocIds == null || pocIds.Count == 0)
            return new List<ReportMembersByPocResponseDto>();

        // Aligns with SQL: CAST(ls.ScheduleDate AS DATE) IN (CAST(GETDATE() AS DATE), DATEADD(DAY, 1, CAST(GETDATE() AS DATE)))
        var windowStart = DateTime.Today;
        var windowEndExclusive = DateTime.Today.AddDays(2);

        var distinctPocIds = pocIds.Distinct().ToList();

        var query =
            from m in _context.Members
            join p in _context.POCs on m.POCId equals p.Id
            join c in _context.Centers on p.CenterId equals c.Id
            join l in _context.Loans on m.Id equals l.MemberId into loans
            from l in loans.DefaultIfEmpty()
            join ls in _context.LoanSchedulers on l.Id equals ls.LoanId into loanSchedulers
            from ls in loanSchedulers.DefaultIfEmpty()
            where !m.IsDeleted
                  && !p.IsDeleted
                  && c.BranchId == branchId
                  && distinctPocIds.Contains(p.Id)
                  && ls != null
                  && ls.Status != null
                  && ls.Status.ToLower() == "not paid"
                  && ls.ScheduleDate >= windowStart
                  && ls.ScheduleDate < windowEndExclusive
            select new ReportMembersByPocResponseDto
            {
                PocId = p.Id,
                MemberId = m.Id,
                MembersFullName = ((m.FirstName ?? string.Empty) + " " +
                                   (m.MiddleName ?? string.Empty) + " " +
                                   (m.LastName ?? string.Empty)).Trim(),
                ActualEmiAmount = ls.ActualEmiAmount,
                ScheduleDate = ls.ScheduleDate,
            };

        return await query
            .Distinct()
            .OrderBy(x => x.PocId)
            .ThenBy(x => x.MemberId)
            .ThenBy(x => x.ScheduleDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ReportSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
  COALESCE(inv.TotalOwnerAmount,0)         AS TotalOwnerAmount,
  COALESCE(inv.TotalInvestorAmount,0)      AS TotalInvestorAmount,
  COALESCE(loan.TotalInsuranceAmount,0)    AS TotalInsuranceAmount,
  COALESCE(loan.TotalProcessingFee,0)      AS TotalProcessingFee,
  COALESCE(ls.ReceivedPrinciple,0)         AS ReceivedPrinciple,
  COALESCE(ls.ReceivedInterest,0)          AS ReceivedInterest,
  COALESCE(ls.OutstandingPrinciple,0)      AS OutstandingPrinciple,
  COALESCE(ls.InterestAccured,0)           AS InterestAccured,
  COALESCE(fees.TotalJoiningFee,0)         AS TotalJoiningFee,
  COALESCE(exp.TotalLedgerExpenseAmount,0) AS TotalLedgerExpenseAmount
FROM
(
  SELECT SUM(CASE WHEN UPPER(U.Role) = 'OWNER' THEN I.Amount ELSE 0 END)    AS TotalOwnerAmount,
         SUM(CASE WHEN UPPER(U.Role) = 'INVESTOR' THEN I.Amount ELSE 0 END) AS TotalInvestorAmount
  FROM Investments I
  JOIN Users U ON I.UserId = U.Id
) inv
CROSS JOIN
(
  SELECT SUM(InsuranceFee) AS TotalInsuranceAmount,
         SUM(ProcessingFee) AS TotalProcessingFee
  FROM Loans
) loan
CROSS JOIN
(
  SELECT
    SUM(CASE WHEN Status IN ('Paid','Partial') THEN PrincipalAmount ELSE 0 END)   AS ReceivedPrinciple,
    SUM(CASE WHEN Status IN ('Paid','Partial') THEN InterestAmount ELSE 0 END)    AS ReceivedInterest,
    SUM(CASE WHEN Status = 'Not Paid' THEN ActualPrincipalAmount ELSE 0 END)      AS OutstandingPrinciple,
    SUM(CASE WHEN Status = 'Not Paid' THEN ActualInterestAmount ELSE 0 END)       AS InterestAccured
  FROM LoanSchedulers
) ls
CROSS JOIN
(
  SELECT SUM(MF.Amount) AS TotalJoiningFee
  FROM MemberMembershipFees MF
) fees
CROSS JOIN
(
  SELECT SUM(Amount) AS TotalLedgerExpenseAmount
  FROM LedgerTransactions
  WHERE TransactionType = 'Expense'
) exp";

        var result = await _context.Database
            .SqlQueryRaw<ReportSummaryResponseDto>(sql)
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? new ReportSummaryResponseDto();
    }

    public async Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId, UserRole? role)
    {
        var rawData = await (
                from m in _context.Members
                join p in _context.POCs on m.POCId equals p.Id
                join c in _context.Centers on p.CenterId equals c.Id
                join b in _context.Branches on c.BranchId equals b.Id
                join l in _context.Loans on m.Id equals l.MemberId
                join ls in _context.LoanSchedulers on l.Id equals ls.LoanId
                where !m.IsDeleted
                   && !p.IsDeleted
                   && b.OrgId == orgId
                   && b.Id == branchId
                   && l.Status == "Active"
                select new
                {
                    m.Id,
                    m.FirstName,
                    m.MiddleName,
                    m.LastName,
                    m.GuardianFirstName,
                    m.GuardianMiddleName,
                    m.GuardianLastName,
                    m.Address1,
                    m.City,
                    m.PhoneNumber,
                    l.LoanAmount,
                    l.DisbursementDate,
                    l.CollectionStartDate,
                    PocFirst = p.FirstName,
                    PocMiddle = p.MiddleName,
                    PocLast = p.LastName,
                    CenterName = c.Name,
                    LsStatus = ls.Status,
                    LsActualEmiAmount = ls.ActualEmiAmount,
                    LsActualPrincipalAmount = ls.ActualPrincipalAmount,
                    LsPrincipalAmount = ls.PrincipalAmount,
                    LsActualInterestAmount = ls.ActualInterestAmount,
                    LsInterestAmount = ls.InterestAmount,
                    LsPaymentAmount = ls.PaymentAmount,
                }
            ).AsNoTracking().ToListAsync();

        var dtoList = rawData
            .GroupBy(x => new
            {
                x.Id,
                x.CenterName,
                x.LoanAmount,
                x.DisbursementDate,
                x.CollectionStartDate,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.GuardianFirstName,
                x.GuardianMiddleName,
                x.GuardianLastName,
                x.Address1,
                x.City,
                x.PhoneNumber,
                x.PocFirst,
                x.PocMiddle,
                x.PocLast
            })
            .Select(g =>
            {
                var unpaid = g.Where(x => x.LsStatus?.ToLower() == "not paid").ToList();
                var paid = g.Where(x => (x.LsStatus?.ToLower() == "paid" || x.LsStatus?.ToLower() == "partial")).ToList();
                var weekly = g.Select(x => x.LsActualEmiAmount).FirstOrDefault();
                var outstanding = unpaid.Count;
                var principleCollected = paid.Sum(x => x.LsPrincipalAmount);
                var interestCollected = paid.Sum(x => x.LsInterestAmount);
                var collected = paid.Sum(x => x.LsPaymentAmount);
                var toBeCollected = g.Sum(x => x.LsActualEmiAmount);
                var osBalance = toBeCollected - collected;

                return new MemberWiseCollectionResponseDto
                {
                    memberId = g.Key.Id,
                    memberName = $"{g.Key.FirstName} {g.Key.MiddleName} {g.Key.LastName}".Trim(),
                    guardianName = $"{g.Key.GuardianFirstName} {g.Key.GuardianMiddleName} {g.Key.GuardianLastName}".Trim(),
                    address = $"{g.Key.Address1} {g.Key.City}".Trim(),
                    phoneNumber = g.Key.PhoneNumber,
                    loanAmount = g.Key.LoanAmount,
                    outstandingWeeks = outstanding,
                    weeklyDueAmount = weekly,
                    asOnOutStanding = outstanding * weekly,
                    collectionDay = g.Key.CollectionStartDate?.DayOfWeek.ToString() ?? string.Empty,
                    attendStaff = $"{g.Key.PocFirst} {g.Key.PocMiddle} {g.Key.PocLast}".Trim(),
                    centerName = g.Key.CenterName,
                    disbursementDate = g.Key.DisbursementDate,
                    principleCollected = principleCollected,
                    interestCollected = interestCollected,
                    collected = collected,
                    toBeCollected = toBeCollected,
                    osBalance = osBalance,
                };
            })
            .OrderBy(x => x.centerName)
            .ThenBy(x => x.memberId)
            .ToList();

        //Expenses logic

        var expenses = await (
            from lt in _context.LedgerTransactions
            join u in _context.Users on lt.PaidFromUserId equals u.Id
            join b in _context.Branches on u.BranchId equals b.Id
            where b.OrgId == orgId
               && b.Id == branchId
               && lt.TransactionType == "Expense"
            orderby lt.PaymentDate
            select new ExpenseResponse
            {
                TransactionType = lt.TransactionType,
                Comments = lt.Comments,
                PaidFromUserId = lt.PaidFromUserId,
                PaymentDate = lt.PaymentDate,
                Amount = lt.Amount,
            }
        ).AsNoTracking().ToListAsync();

        // Assign serial numbers after fetch
        for (int idx = 0; idx < expenses.Count; idx++)
            expenses[idx].Id = idx + 1;

         List<LedgerReportDto> ledgers = null;

        if (role == UserRole.Owner)
        {
            //Ledger Balance logic
            ledgers = await (
                from l in _context.Ledgers
                join u in _context.Users on l.UserId equals u.Id
                where u.OrgId == orgId
                orderby u.FirstName, u.LastName
                select new LedgerReportDto
                {
                    UserName = (u.FirstName + " " + u.LastName).Trim(),
                    Amount = l.Amount,
                    InsuranceAmount = l.InsuranceAmount,
                    ClaimedAmount = l.ClaimedAmount,
                }
            ).AsNoTracking().ToListAsync();

            // Assign serial numbers
            for (int idx = 0; idx < ledgers.Count; idx++)
                ledgers[idx].id = idx + 1;
        }

        return Generate(dtoList, expenses, ledgers);
    }

    public byte[] Generate(List<MemberWiseCollectionResponseDto> data, List<ExpenseResponse> expenses = null, List<LedgerReportDto> ledgers = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Member Wise Collection");

        var titleFill = XLColor.FromHtml("#1F4E79");
        var groupFill = XLColor.FromHtml("#D6E4F0");
        var groupFont = XLColor.FromHtml("#1F4E79");
        var dayTotalFill = XLColor.FromHtml("#BDD7EE");
        var grandFill = XLColor.FromHtml("#1F4E79");
        var altFill = XLColor.FromHtml("#EBF5FB");

        string[] headers =
        [
            "Member Code", "Member Name", "Guardian Name", "Village",
            "Contact No", "Loan Amount", "Outstanding Weeks",
            "Weekly Due Amount", "As On Outstanding",
            "Collection Day", "Attend Staff"
        ];
        int[] colWidths = [13, 24, 24, 34, 14, 13, 14, 16, 16, 14, 16];

        int row = 1;
        var titleRange = ws.Range(row, 1, row, 11);
        titleRange.Merge();
        titleRange.Value = "Member Wise Collection Sheet";
        titleRange.Style
            .Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(titleFill)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(row).Height = 28;

        row++;
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style
                .Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(titleFill)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        ws.Row(row).Height = 32;

        var byDay = data
            .GroupBy(x => x.collectionDay)
            .OrderBy(g => g.Key);

        // ── CHANGE 1: Renamed from dayDataRows to dayTotalRowNumbers ──────
        // Old: var dayDataRows = new List<(string day, int startRow, int endRow)>();
        // New: tracks only the row number where each day total is written
        var dayTotalRowNumbers = new List<int>();

        foreach (var dayGroup in byDay)
        {
            int dayBlockStart = row + 1;

            var byCentre = dayGroup
                .GroupBy(x => x.centerName)
                .OrderBy(g => g.Key);

            // ── CHANGE 2: Added memberRowRanges to track only member rows ─
            // This is new — used to build day total formula without centre headers
            var memberRowRanges = new List<(int start, int end)>();

            foreach (var centreGroup in byCentre)
            {
                row++;
                int centreHeaderRow = row;

                var centreRange = ws.Range(row, 1, row, 5);
                centreRange.Merge();
                centreRange.Value = centreGroup.Key;
                centreRange.Style
                    .Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(groupFont)
                    .Fill.SetBackgroundColor(groupFill)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                for (int col = 6; col <= 11; col++)
                {
                    ws.Cell(row, col).Style
                        .Fill.SetBackgroundColor(groupFill)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
                ws.Row(row).Height = 18;

                int memberStart = row + 1;
                int i = 0;

                foreach (var m in centreGroup)
                {
                    row++;
                    var bg = i++ % 2 == 0 ? altFill : XLColor.White;

                    object[] values =
                    [
                        m.memberId,         m.memberName,      m.guardianName,
                        m.address,          m.phoneNumber,     m.loanAmount,
                        m.outstandingWeeks, m.weeklyDueAmount, m.asOnOutStanding,
                        m.collectionDay,    m.attendStaff
                    ];

                    for (int col = 1; col <= values.Length; col++)
                    {
                        var cell = ws.Cell(row, col);
                        cell.Value = XLCellValue.FromObject(values[col - 1]);
                        cell.Style
                            .Font.SetFontSize(9)
                            .Fill.SetBackgroundColor(bg)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                        if (col is 6 or 7 or 8 or 9)
                            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                        if (col is 6 or 8 or 9)
                            cell.Style.NumberFormat.SetFormat("#,##0.00");
                    }
                    ws.Row(row).Height = 16;
                }

                int memberEnd = row;

                // ── CHANGE 3: Track member row range for this centre ──────
                // Old: nothing tracked here
                // New: add to memberRowRanges so day total can skip centre headers
                memberRowRanges.Add((memberStart, memberEnd));

                ws.Cell(centreHeaderRow, 9).FormulaA1 = $"SUM(I{memberStart}:I{memberEnd})";
                ws.Cell(centreHeaderRow, 9).Style
                    .Font.SetBold(true).Font.SetFontColor(groupFont)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                    .NumberFormat.SetFormat("#,##0.00");
            }

            int dayBlockEnd = row;

            row++;
            int dayTotalRow = row;

            // ── CHANGE 4: Track the day total row number ──────────────────
            // Old: dayDataRows.Add((dayGroup.Key, dayBlockStart, dayBlockEnd));
            // New: just track the row number for grand total formula
            dayTotalRowNumbers.Add(dayTotalRow);

            var dayTotalRange = ws.Range(row, 1, row, 3);
            dayTotalRange.Merge();
            dayTotalRange.Value = $"{dayGroup.Key} Total";
            dayTotalRange.Style
                .Font.SetBold(true).Font.SetFontSize(10).Font.SetFontColor(groupFont)
                .Fill.SetBackgroundColor(dayTotalFill)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            for (int col = 4; col <= 11; col++)
            {
                ws.Cell(row, col).Style
                    .Fill.SetBackgroundColor(dayTotalFill)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }

            // ── CHANGE 5: Day total formula now skips centre header rows ──
            // Old: ws.Cell(row, 9).FormulaA1 = $"SUM(I{dayBlockStart}:I{dayBlockEnd})";
            //      — this included centre header rows which have subtotal formulas = double count
            // New: sum only actual member row ranges, centre headers skipped entirely
            var dayFormula = string.Join("+", memberRowRanges.Select(r => $"SUM(I{r.start}:I{r.end})"));
            ws.Cell(row, 9).FormulaA1 = dayFormula;
            ws.Cell(row, 9).Style
                .Font.SetBold(true).Font.SetFontColor(groupFont)
                .NumberFormat.SetFormat("#,##0.00");

            ws.Row(row).Height = 18;
        }

        row++;
        var grandRange = ws.Range(row, 1, row, 3);
        grandRange.Merge();
        grandRange.Value = "Grand Total";
        grandRange.Style
            .Font.SetBold(true).Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(grandFill)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        for (int col = 4; col <= 11; col++)
        {
            ws.Cell(row, col).Style
                .Fill.SetBackgroundColor(grandFill)
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        }

        // ── CHANGE 6: Grand total formula sums only day total rows ────────
        // Old: ws.Cell(row, 9).FormulaA1 = $"SUM(I3:I{row - 1})";
        //      — included everything: member rows + centre subtotals + day totals = triple count
        // New: sums only the day total rows e.g. I10+I25+I40
        var grandFormula = string.Join("+", dayTotalRowNumbers.Select(r => $"I{r}"));
        ws.Cell(row, 9).FormulaA1 = grandFormula;
        ws.Cell(row, 9).Style
            .Font.SetBold(true).Font.SetFontColor(XLColor.White)
            .NumberFormat.SetFormat("#,##0.00");
        ws.Row(row).Height = 22;

        for (int col = 1; col <= colWidths.Length; col++)
            ws.Column(col).Width = colWidths[col - 1];

        ws.SheetView.FreezeRows(2);

        GenerateRepaymentSheet(wb, data);

        if (expenses != null && expenses.Count > 0)
            GenerateExpensesSheet(wb, expenses);

        if (ledgers != null && ledgers.Count > 0)
            GenerateLedgerSheet(wb, ledgers);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();

    }

    public void GenerateRepaymentSheet(XLWorkbook wb, List<MemberWiseCollectionResponseDto> data)
    {
        var ws = wb.Worksheets.Add("Repayment sheet");

        var titleFill = XLColor.FromHtml("#1F4E79");
        var headerFill = XLColor.FromHtml("#D6E4F0");
        var altFill = XLColor.FromHtml("#EBF5FB");
        var redFont = XLColor.FromHtml("#FF0000");
        var boldBlue = XLColor.FromHtml("#1F4E79");

        string[] rowLabels =
        [
            "Repayment Sheet",      // row 1 - title
        "Principle Collected",  // row 2
        "Interest Collected",   // row 3
        "",                     // row 4 - spacer
        "Member code",          // row 5
        "Member name",          // row 6
        "Village",              // row 7
        "Disb date",            // row 8
        "B/F Loan Amount",      // row 9
        "1 St Loan",            // row 10
        "Fully Payments",       // row 11
        "",                     // row 12 - spacer
        "As On Loan",           // row 13
        "",                     // row 14 - spacer
        "B/F OS",               // row 15
        "",                     // row 16 - spacer
        "Tobe collected",       // row 17
        "Collected",            // row 28
        "O/s balance",          // row 21
        ];

        int totalCols = data.Count + 1; // col A labels + one col per member

        // ── Title row — merge FIRST before writing anything else ──────
        var titleMerge = ws.Range(1, 1, 1, totalCols);
        titleMerge.Merge();
        ws.Cell(1, 1).Value = "Repayment Sheet";
        ws.Cell(1, 1).Style
            .Font.SetBold(true).Font.SetFontSize(12).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(titleFill)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 24;

        // ── Row labels in col A ───────────────────────────────────────
        for (int r = 2; r <= rowLabels.Length; r++) // ← start from row 2, row 1 is title
        {
            var cell = ws.Cell(r, 1);
            cell.Value = rowLabels[r - 1];
            cell.Style
                .Font.SetBold(true)
                .Font.SetFontSize(9)
                .Font.SetFontColor(boldBlue)
                .Fill.SetBackgroundColor(headerFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            if (rowLabels[r - 1] == "B/F OS")
                cell.Style.Font.SetFontColor(redFont);
        }

        ws.Column(1).Width = 20;

        // ── Write each member as a column ─────────────────────────────
        int colIndex = 2;
        foreach (var m in data)
        {
            ws.Column(colIndex).Width = 14;

            // Style title cell for this column (already merged, just style)
            ws.Cell(1, colIndex).Style
                .Fill.SetBackgroundColor(titleFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            void WriteCell(int rowNum, object value, bool isRed = false, bool isBold = false)
            {
                var cell = ws.Cell(rowNum, colIndex);
                cell.Value = XLCellValue.FromObject(value);
                cell.Style
                    .Font.SetFontSize(9)
                    .Font.SetBold(isBold)
                    .Fill.SetBackgroundColor(colIndex % 2 == 0 ? altFill : XLColor.White)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                if (isRed)
                    cell.Style.Font.SetFontColor(redFont);

                if (value is decimal or int or double)
                    cell.Style.NumberFormat.SetFormat("#,##0.00");
            }

            WriteCell(2, m.principleCollected);
            WriteCell(3, m.interestCollected);
            WriteCell(4, "");
            WriteCell(5, $"NM{m.memberId:D4}");
            WriteCell(6, m.memberName);
            WriteCell(7, m.address);
            WriteCell(8, m.disbursementDate?.ToString("dd.MM.yyyy") ?? "");
            WriteCell(9, (decimal)0);
            WriteCell(10, m.loanAmount);
            WriteCell(11, (decimal)0);
            WriteCell(12, (decimal)0);
            WriteCell(13, (decimal)0);
            WriteCell(14, "");
            WriteCell(15, m.loanAmount, isBold: true);
            WriteCell(16, "");
            WriteCell(17, (decimal)0, isRed: true);
            WriteCell(18, "");
            WriteCell(19, m.toBeCollected);
            WriteCell(20, m.collected);
            WriteCell(21, m.osBalance, isBold: true);

            colIndex++;
        }

        // ── Freeze: row 1 (title) + col A (labels) ────────────────────
        ws.SheetView.Freeze(1, 1); // ← single call freezes both row and column
    }


    public void GenerateExpensesSheet(XLWorkbook wb, List<ExpenseResponse> expenses)
    {
        var ws = wb.Worksheets.Add("Expenses");

        // ── Colors ────────────────────────────────────────────────
        var titleFill = XLColor.FromHtml("#1F4E79");
        var headerFill = XLColor.FromHtml("#BDB07A");   // olive/khaki matching screenshot
        var headerFont = XLColor.White;
        var totalFill = XLColor.FromHtml("#BDB07A");
        var altFill = XLColor.FromHtml("#F5F5F0");

        // ── Column widths ──────────────────────────────────────────
        ws.Column(1).Width = 6;    // Sl.N
        ws.Column(2).Width = 20;   // Type
        ws.Column(3).Width = 60;   // Particulars
        ws.Column(4).Width = 14;   // Date
        ws.Column(5).Width = 14;   // Amount

        // ── Title row ─────────────────────────────────────────────
        int row = 1;
        var titleRange = ws.Range(row, 1, row, 5);
        titleRange.Merge();
        titleRange.Value = "Expenses";
        titleRange.Style
            .Font.SetBold(true)
            .Font.SetFontSize(13)
            .Font.SetFontColor(XLColor.FromHtml("#1F4E79"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(row).Height = 22;

        // ── Header row ────────────────────────────────────────────
        row++;
        string[] headers = ["Sl.N", "Type", "Particulars", "Date", "Amount"];
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style
                .Font.SetBold(true)
                .Font.SetFontSize(10)
                .Font.SetFontColor(headerFont)
                .Fill.SetBackgroundColor(headerFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }
        ws.Row(row).Height = 22;

        // Enable autofilter on header row
        ws.Range(row, 1, row, 5).SetAutoFilter();

        // ── Data rows — always write 10 rows minimum ───────────────
        int dataStartRow = row + 1;
        int totalRows = Math.Max(expenses.Count, 10);

        for (int i = 0; i < totalRows; i++)
        {
            row++;
            var bg = i % 2 == 0 ? XLColor.White : altFill;

            // Sl.N
            var slCell = ws.Cell(row, 1);
            if (i < expenses.Count)
                slCell.Value = expenses[i].Id;
            slCell.Style
                .Font.SetFontSize(9)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Type
            var typeCell = ws.Cell(row, 2);
            if (i < expenses.Count)
            {
                typeCell.Value = expenses[i].TransactionType;
                typeCell.Style.Font.SetBold(true);
            }
            typeCell.Style
                .Font.SetFontSize(9)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Particulars
            var partCell = ws.Cell(row, 3);
            if (i < expenses.Count)
                partCell.Value = expenses[i].Comments;
            partCell.Style
                .Font.SetFontSize(9)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Date
            var dateCell = ws.Cell(row, 4);
            if (i < expenses.Count && expenses[i].PaymentDate.HasValue)
            {
                dateCell.Value = expenses[i].PaymentDate.Value.ToString("dd/MM/yyyy");
            }
            dateCell.Style
                .Font.SetFontSize(9)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Amount
            var amtCell = ws.Cell(row, 5);
            if (i < expenses.Count)
            {
                amtCell.Value = expenses[i].Amount;
                amtCell.Style.NumberFormat.SetFormat("#,##0.00");
            }
            amtCell.Style
                .Font.SetFontSize(9)
                .Fill.SetBackgroundColor(bg)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            ws.Row(row).Height = 16;
        }

        int dataEndRow = row;

        // ── Total row ──────────────────────────────────────────────
        row++;
        for (int col = 1; col <= 5; col++)
        {
            var cell = ws.Cell(row, col);
            cell.Style
                .Fill.SetBackgroundColor(totalFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Font.SetBold(true)
                .Font.SetFontSize(10);
        }

        ws.Cell(row, 4).Value = "Total:-";
        ws.Cell(row, 4).Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
            .Font.SetBold(true);

        ws.Cell(row, 5).FormulaA1 = $"SUM(E{dataStartRow}:E{dataEndRow})";
        ws.Cell(row, 5).Style
            .NumberFormat.SetFormat("#,##0.00")
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Row(row).Height = 18;

        ws.SheetView.FreezeRows(2); // freeze title + header
    }

    private void GenerateLedgerSheet(XLWorkbook wb, List<LedgerReportDto> ledgers)
    {
        var ws = wb.Worksheets.Add("Ledger Balance");

        // ── Colors ────────────────────────────────────────────────
        var titleFill = XLColor.FromHtml("#1F4E79");
        var headerFill = XLColor.FromHtml("#BDB07A");
        var totalFill = XLColor.FromHtml("#BDB07A");
        var altFill = XLColor.FromHtml("#F5F5F0");

        // ── Column widths ──────────────────────────────────────────
        ws.Column(1).Width = 6;    // Sl.N
        ws.Column(2).Width = 28;   // User Name
        ws.Column(3).Width = 18;   // Amount
        ws.Column(4).Width = 18;   // Insurance Amount
        ws.Column(5).Width = 18;   // Claimed Amount

        // ── Title row ─────────────────────────────────────────────
        int row = 1;
        var titleRange = ws.Range(row, 1, row, 5);
        titleRange.Merge();
        titleRange.Value = "Ledger Balance Sheet";
        titleRange.Style
            .Font.SetBold(true)
            .Font.SetFontSize(13)
            .Font.SetFontColor(XLColor.FromHtml("#1F4E79"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(row).Height = 22;

        // ── Header row ────────────────────────────────────────────
        row++;
        string[] headers = ["Sl.N", "User Name", "Amount", "Insurance Amount", "Claimed Amount"];
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style
                .Font.SetBold(true)
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(headerFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }
        ws.Row(row).Height = 22;
        ws.Range(row, 1, row, 5).SetAutoFilter();

        // ── Data rows ─────────────────────────────────────────────
        int dataStartRow = row + 1;

        for (int i = 0; i < ledgers.Count; i++)
        {
            row++;
            var bg = i % 2 == 0 ? XLColor.White : altFill;
            var l = ledgers[i];

            void WriteCell(int col, object value, bool isRed = false, bool isBold = false)
            {
                var cell = ws.Cell(row, col);
                cell.Value = XLCellValue.FromObject(value);
                cell.Style
                    .Font.SetFontSize(9)
                    .Font.SetBold(isBold)
                    .Fill.SetBackgroundColor(bg)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                if (isRed)
                    cell.Style.Font.SetFontColor(XLColor.Red);

                if (value is decimal or double)
                    cell.Style.NumberFormat.SetFormat("#,##0.00");
            }

            WriteCell(1, l.id);
            WriteCell(2, l.UserName);
            WriteCell(3, l.Amount,
                isRed: l.Amount < 0);                    // negative amounts in red
            WriteCell(4, l.InsuranceAmount,
                isRed: l.InsuranceAmount < 0);            // negative in red (like -23543.52)
            WriteCell(5, l.ClaimedAmount);

            // Right-align numeric columns
            ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Row(row).Height = 16;
        }

        int dataEndRow = row;

        // ── Total row ──────────────────────────────────────────────
        row++;
        for (int col = 1; col <= 5; col++)
        {
            ws.Cell(row, col).Style
                .Fill.SetBackgroundColor(totalFill)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Font.SetBold(true)
                .Font.SetFontSize(10);
        }

        ws.Cell(row, 2).Value = "Total:-";
        ws.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        // Total for Amount, InsuranceAmount, ClaimedAmount
        ws.Cell(row, 3).FormulaA1 = $"SUM(C{dataStartRow}:C{dataEndRow})";
        ws.Cell(row, 4).FormulaA1 = $"SUM(D{dataStartRow}:D{dataEndRow})";
        ws.Cell(row, 5).FormulaA1 = $"SUM(E{dataStartRow}:E{dataEndRow})";

        for (int col = 3; col <= 5; col++)
        {
            ws.Cell(row, col).Style
                .NumberFormat.SetFormat("#,##0.00")
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        }

        ws.Row(row).Height = 18;
        ws.SheetView.FreezeRows(2);
    }
}
