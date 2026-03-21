using MicroCredit.Domain.Entities;
namespace MicroCredit.Domain.Interfaces.Repository;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default);


}