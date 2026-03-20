using MicroCredit.Domain.Model.Loan;

namespace MicroCredit.Domain.Interfaces.Services;

public interface ILoansService
{
    Task<IEnumerable<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchid, CancellationToken cancellationToken = default);
}