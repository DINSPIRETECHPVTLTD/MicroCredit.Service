using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Loan;
namespace MicroCredit.Domain.Interfaces.Repository;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchId, CancellationToken cancellationToken = default);


    Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default);


}