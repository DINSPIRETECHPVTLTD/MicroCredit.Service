using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;
public interface ILoanSchedulersRepository
{
    Task<LoanScheduler?> GetByLoanId(int LoanId, CancellationToken cancellationToken);

}

