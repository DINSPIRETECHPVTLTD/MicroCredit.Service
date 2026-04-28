using MicroCredit.Domain.Model.Loan;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Services;

public interface ILoansService
{
    Task<IEnumerable<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchid, CancellationToken cancellationToken = default);

    Task<LoanResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Loan> AddLoanAsync(CreateLoanRequest request, int userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default);

    Task<LoanResponse> UpdateLoanStatusAsync(int loanId, string status, int userId, CancellationToken cancellationToken = default);

    Task<ClaimLoanResponse> ClaimLoanAsync(int loanId, int userId, CancellationToken cancellationToken = default);

    Task<CloseLoanResponse> CloseLoanAsync(int loanId, int userId, CancellationToken cancellationToken = default);
}