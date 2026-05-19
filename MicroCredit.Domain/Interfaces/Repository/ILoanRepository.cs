using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Loan;
namespace MicroCredit.Domain.Interfaces.Repository;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchId, CancellationToken cancellationToken = default);

    Task<bool> HasOpenSchedulersAsync(int loanId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenLoanForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// For each member id that has at least one non-deleted loan in Active/Pending/Claimed status (same as GetLoanByMemId),
    /// returns the maximum loan id (matches Add Loan UI navigation).
    /// </summary>
    Task<Dictionary<int, int>> GetMaxViewableLoanIdsByMemberIdsAsync(
        IReadOnlyList<int> memberIds,
        CancellationToken cancellationToken = default);

    Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default);


}