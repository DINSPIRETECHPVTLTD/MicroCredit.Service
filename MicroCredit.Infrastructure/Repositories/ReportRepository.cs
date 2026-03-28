using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Report;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                  && ls.ScheduleDate >= windowStart
                  && ls.ScheduleDate < windowEndExclusive
            select new ReportMembersByPocResponseDto
            {
                MemberId = m.Id,
                MembersFullName = ((m.FirstName ?? string.Empty) + " " +
                                   (m.MiddleName ?? string.Empty) + " " +
                                   (m.LastName ?? string.Empty)).Trim(),
                ActualEmiAmount = ls.ActualEmiAmount,
                ScheduleDate = ls.ScheduleDate,
            };

        return await query
            .Distinct()
            .OrderBy(x => x.MemberId)
            .ThenBy(x => x.ScheduleDate)
            .AsNoTracking()
            .ToListAsync();
    }
}
