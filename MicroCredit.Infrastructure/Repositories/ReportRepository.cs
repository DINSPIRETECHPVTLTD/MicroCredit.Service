using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Report;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Drawing;

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

    public async Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId)
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
                    l.CollectionStartDate,
                    PocFirst = p.FirstName,
                    PocMiddle = p.MiddleName,
                    PocLast = p.LastName,
                    CenterName = c.Name,
                    LsStatus = ls.Status,
                    LsActualEmiAmount = ls.ActualEmiAmount,
                }
            ).AsNoTracking().ToListAsync();

        var dtoList = rawData
            .GroupBy(x => new
            {
                x.Id,
                x.CenterName,
                x.LoanAmount,
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
                var weekly = g.Select(x => x.LsActualEmiAmount).FirstOrDefault();
                var outstanding = unpaid.Count;

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
                };
            })
            .OrderBy(x => x.centerName)
            .ThenBy(x => x.memberId)
            .ToList();

        return Generate(dtoList);
    }

    public byte[] Generate(List<MemberWiseCollectionResponseDto> data)
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

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();

    }
}
