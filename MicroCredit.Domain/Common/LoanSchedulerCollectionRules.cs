using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Common;

/// <summary>
/// Dual-era LoanScheduler collection predicates (legacy same-row Partial vs new payment children).
/// </summary>
public static class LoanSchedulerCollectionRules
{
    public static bool IsBase(int? parentLoanSchedulerId, int subInstallmentSequence) =>
        parentLoanSchedulerId == null && subInstallmentSequence == 0;

    public static bool IsBase(LoanScheduler row) =>
        IsBase(row.ParentLoanSchedulerId, row.SubInstallmentSequence);

    public static bool IsPaymentHistory(int? parentLoanSchedulerId, LoanSchedulerStatus status, decimal paymentAmount) =>
        parentLoanSchedulerId != null
        && paymentAmount > 0
        && (status == LoanSchedulerStatus.Partial || status == LoanSchedulerStatus.Paid);

    public static bool IsPaymentHistory(LoanScheduler row) =>
        IsPaymentHistory(row.ParentLoanSchedulerId, row.Status, row.PaymentAmount);

    public static bool IsLegacyPartial(LoanScheduler row, bool hasPaymentChildren) =>
        IsBase(row)
        && row.Status == LoanSchedulerStatus.Partial
        && row.PaymentAmount > 0
        && !hasPaymentChildren;

    public static bool IsCollectible(LoanScheduler row) =>
        IsBase(row)
        && row.Status == LoanSchedulerStatus.NotPaid
        && row.ActualEmiAmount > 0;

    /// <summary>
    /// Overdue posting always sets <see cref="LoanScheduler.PaymentDate"/> and carries the due
    /// amounts onto the next base installment in the same transaction. A committed Overdue row
    /// with PaymentDate and a later base therefore has already been transferred and must not
    /// block sequential posting. Overdue without PaymentDate, or with no later base, is treated
    /// as untransferred and still blocks.
    /// </summary>
    public static bool IsUntransferredOverdue(
        LoanSchedulerStatus status,
        DateTime? paymentDate,
        bool hasLaterBaseInstallment)
    {
        if (status != LoanSchedulerStatus.Overdue)
            return false;
        if (paymentDate == null)
            return true;
        return !hasLaterBaseInstallment;
    }

    /// <summary>
    /// Earlier base blocks posting of a later installment when it still has collectible Not Paid
    /// outstanding, or an Overdue balance that has not been carried forward.
    /// </summary>
    public static bool BlocksSequentialCollection(
        LoanSchedulerStatus status,
        decimal actualEmiAmount,
        DateTime? paymentDate,
        bool hasLaterBaseInstallment)
    {
        if (status == LoanSchedulerStatus.NotPaid && actualEmiAmount > 0)
            return true;
        return IsUntransferredOverdue(status, paymentDate, hasLaterBaseInstallment);
    }

    public static decimal CollectedAmount(LoanScheduler row, bool hasPaymentChildren)
    {
        if (IsPaymentHistory(row))
            return row.PaymentAmount;
        if (IsBase(row) && row.Status == LoanSchedulerStatus.Paid && row.PaymentAmount > 0)
            return row.PaymentAmount;
        if (IsLegacyPartial(row, hasPaymentChildren))
            return row.PaymentAmount;
        return 0m;
    }

    public static decimal OutstandingAmount(LoanScheduler row)
    {
        if (IsCollectible(row))
            return row.ActualEmiAmount;
        return 0m;
    }

    public static string FormatInstallmentLabel(int installmentNo, int subInstallmentSequence) =>
        subInstallmentSequence > 0 ? $"{installmentNo}_{subInstallmentSequence}" : installmentNo.ToString();
}
