using MicroCredit.Domain.Model.Loan;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Services;

public interface ILoansService
{
    Task<IEnumerable<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LoanResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Loan> AddLoanAsync(CreateLoanRequest request, int userId, CancellationToken cancellationToken = default);
}