using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;
public class LoanSchedulersRepository : ILoanSchedulersRepository
{
    private readonly MicroCreditDbContext _context;
    public LoanSchedulersRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<LoanScheduler?> GetByLoanId(int Loanid, CancellationToken cancellationToken)
    {
        var scheduler = await _context.LoanSchedulers
        .FirstOrDefaultAsync(ls => ls.LoanId == Loanid, cancellationToken);

        if (scheduler == null)
        {
            return null;
        }

        return scheduler;
    }

    public async Task AddRangeAsync(IEnumerable<LoanScheduler> schedules, CancellationToken cancellationToken = default)
    {
        await _context.LoanSchedulers.AddRangeAsync(schedules, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<LoanScheduler>> GetLoanSchedulersByIdAsync(int Loanid, CancellationToken cancellationToken)
    {
        var schedulers = await _context.LoanSchedulers.Where(ls => ls.LoanId == Loanid).ToListAsync(cancellationToken);

        if (schedulers == null)
        {
            throw new Exception($"LoanSchedule with ID {Loanid} not found.");
        }
        return schedulers;
    }

    public async Task<IReadOnlyList<LoanScheduler>> GetFutureUnpaidByPocIdAsync(
        int pocId,
        DateTime fromDate,
        CancellationToken cancellationToken = default)
    {
        return await (
            from schedule in _context.LoanSchedulers
            join loan in _context.Loans on schedule.LoanId equals loan.Id
            join member in _context.Members on loan.MemberId equals member.Id
            where member.POCId == pocId
                  && !member.IsDeleted
                  && !loan.IsDeleted
                  && schedule.Status == LoanSchedulerStatus.NotPaid
                  && schedule.ScheduleDate.Date >= fromDate.Date
            orderby schedule.LoanId, schedule.InstallmentNo
            select schedule
        ).ToListAsync(cancellationToken);
    }


}
