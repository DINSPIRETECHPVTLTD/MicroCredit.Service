using MicroCredit.Domain.Entities;
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

    /// <summary>
    /// Retired. Same-row Partial updates are no longer supported.
    /// Partial posting must shrink the Not Paid base and insert a payment child.
    /// </summary>
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

    /// <summary>Locks the base LoanScheduler row (UPDLOCK, ROWLOCK) and returns it.</summary>
    Task<LoanScheduler?> LockAndGetBaseSchedulerAsync(
        int loanSchedulerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conditionally shrinks base outstanding after a partial payment.
    /// Returns rows affected (0 = concurrency conflict).
    /// </summary>
    Task<int> TryShrinkBaseForPartialAsync(
        int loanSchedulerId,
        decimal expectedActualEmi,
        decimal expectedActualPrincipal,
        decimal expectedActualInterest,
        decimal remainingEmi,
        decimal remainingPrincipal,
        decimal remainingInterest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conditionally closes base after final settlement (Actual/Payment = 0, Status = Paid).
    /// Returns rows affected (0 = concurrency conflict).
    /// </summary>
    Task<int> TryCloseBaseAfterFinalSettleAsync(
        int loanSchedulerId,
        decimal expectedActualEmi,
        decimal expectedActualPrincipal,
        decimal expectedActualInterest,
        CancellationToken cancellationToken = default);

    Task<int> GetNextSubInstallmentSequenceAsync(
        int parentLoanSchedulerId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPaymentChildrenAsync(
        int baseLoanSchedulerId,
        CancellationToken cancellationToken = default);

    Task<int> InsertPaymentChildAsync(
        LoanScheduler child,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts earlier base rows that block sequential posting:
    /// Parent null, Seq 0, InstallmentNo &lt; beforeInstallmentNo, and
    /// (NotPaid with ActualEmi &gt; 0) OR Overdue that has not been carried forward
    /// (no PaymentDate and/or no later base installment).
    /// </summary>
    Task<int> CountBlockingEarlierBasesAsync(
        int loanId,
        int beforeInstallmentNo,
        CancellationToken cancellationToken = default);
}
