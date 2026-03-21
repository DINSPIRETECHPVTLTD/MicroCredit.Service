using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;
public interface ILoanSchedulersRepository
{
    Task<LoanScheduler?> GetByLoanId(int LoanId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<LoanScheduler> schedules, CancellationToken cancellationToken);
    Task<IEnumerable<LoanScheduler>> GetLoanSchedulersByIdAsync(int Loanid, CancellationToken cancellationToken);

}

