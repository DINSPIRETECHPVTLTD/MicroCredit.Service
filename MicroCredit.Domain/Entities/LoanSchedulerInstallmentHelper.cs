namespace MicroCredit.Domain.Entities;

/// <summary>
/// Display and ordering helpers for base installments and partial sub-installments (6, 6_1, 6_2).
/// LoanSchedulerId is always an integer identity; labels like "6_1" are display-only.
/// </summary>
public static class LoanSchedulerInstallmentHelper
{
    public static string FormatInstallmentLabel(int installmentNo, int subInstallmentSequence)
    {
        if (subInstallmentSequence <= 0)
            return installmentNo.ToString();

        return $"{installmentNo}_{subInstallmentSequence}";
    }

    public static int CompareInstallmentOrder(int installmentNoA, int subSequenceA, int installmentNoB, int subSequenceB)
    {
        var installmentCompare = installmentNoA.CompareTo(installmentNoB);
        return installmentCompare != 0 ? installmentCompare : subSequenceA.CompareTo(subSequenceB);
    }

    public static int CompareInstallmentOrder(LoanScheduler a, LoanScheduler b)
        => CompareInstallmentOrder(a.InstallmentNo, a.SubInstallmentSequence, b.InstallmentNo, b.SubInstallmentSequence);
}
