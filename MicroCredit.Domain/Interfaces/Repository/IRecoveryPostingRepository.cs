using MicroCredit.Domain.Model.RecoveryPosting;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IRecoveryPostingRepository
{
    Task<IReadOnlyList<RecoveryPostingSchedulerResponse>> GetSchedulersAsync(
        DateTime scheduleDate,
        int orgId,
        int branchId,
        int? centerId,
        int? pocId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryPostingSchedulerSnapshot>> GetLoanSchedulerSnapshotsForBranchByIdsAsync(
        IReadOnlyCollection<int> loanSchedulerIds,
        int orgId,
        int branchId,
        CancellationToken cancellationToken = default);

    Task<int?> GetNextUnpaidLoanSchedulerIdAsync(
        int loanId,
        int afterInstallmentNo,
        CancellationToken cancellationToken = default);

    Task ApplyFullRecoveryPaymentAsync(
        int loanSchedulerId,
        decimal paymentEmi,
        decimal principalPaid,
        decimal interestPaid,
        int collectedBy,
        string? paymentMode,
        string? comments,
        CancellationToken cancellationToken = default);

    Task ApplyPartialRecoveryPaymentAsync(
        int loanSchedulerId,
        decimal amountPaid,
        decimal principalPaid,
        decimal interestPaid,
        int collectedBy,
        string? paymentMode,
        string? comments,
        CancellationToken cancellationToken = default);

    Task ApplyOverdueRecoveryAsync(
        int loanSchedulerId,
        int collectedBy,
        string? comments,
        CancellationToken cancellationToken = default);

    Task AddCarryForwardToScheduleAsync(
        int loanSchedulerId,
        decimal shortfallPrincipal,
        decimal shortfallInterest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Not Paid installment after the last schedule so overdue/shortfall
    /// amounts can be carried forward. Also increments Loan.NoOfTerms.
    /// Returns the new LoanSchedulerId.
    /// </summary>
    Task<int> CreateNextCarryForwardScheduleAsync(
        int loanId,
        DateTime scheduleDate,
        decimal actualPrincipalAmount,
        decimal actualInterestAmount,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<string?> GetLoanCollectionTermAsync(
        int loanId,
        CancellationToken cancellationToken = default);
}
