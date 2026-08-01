namespace MicroCredit.Application.Utilities;

/// <summary>
/// Proportional EMI split for partial payments — mirrors frontend recoveryPostingCalculations.ts.
/// </summary>
public static class EmiAmountSplitter
{
    public sealed record SplitResult(
        decimal PaidEmi,
        decimal PaidPrincipal,
        decimal PaidInterest,
        decimal? PaidSaving,
        decimal RemainEmi,
        decimal RemainPrincipal,
        decimal RemainInterest,
        decimal? RemainSaving);

    public static SplitResult SplitPartialPayment(
        decimal dueEmi,
        decimal duePrincipal,
        decimal dueInterest,
        decimal payment,
        decimal? dueSaving = null)
    {
        if (payment <= 0)
            throw new ArgumentException("Payment must be greater than zero.", nameof(payment));

        if (dueEmi > 0 && payment > dueEmi)
            throw new ArgumentException("Payment cannot exceed scheduled EMI.", nameof(payment));

        var dueTotalFromPi = duePrincipal + dueInterest;
        decimal paidPrincipal;
        decimal paidInterest;

        if (dueTotalFromPi > 0)
        {
            var principalRatio = duePrincipal / dueTotalFromPi;
            paidPrincipal = Round2(payment * principalRatio);
            paidInterest = Round2(payment - paidPrincipal);
        }
        else if (dueEmi > 0)
        {
            var principalRatio = duePrincipal / dueEmi;
            paidPrincipal = Round2(payment * principalRatio);
            paidInterest = Round2(payment - paidPrincipal);
        }
        else
        {
            paidPrincipal = Round2(payment);
            paidInterest = 0;
        }

        var remainPrincipal = Round2(duePrincipal - paidPrincipal);
        var remainInterest = Round2(dueInterest - paidInterest);
        var remainEmi = Round2(dueEmi - payment);

        if (remainPrincipal < 0 || remainInterest < 0 || remainEmi < 0)
            throw new InvalidOperationException("Partial payment split produced a negative remainder.");

        decimal? paidSaving = null;
        decimal? remainSaving = null;
        if (dueSaving.HasValue)
        {
            var saving = dueSaving.Value;
            if (dueEmi > 0 && saving > 0)
            {
                paidSaving = Round2(payment * (saving / dueEmi));
                remainSaving = Round2(saving - paidSaving.Value);
            }
            else
            {
                paidSaving = 0;
                remainSaving = saving;
            }
        }

        return new SplitResult(
            PaidEmi: Round2(payment),
            PaidPrincipal: paidPrincipal,
            PaidInterest: paidInterest,
            PaidSaving: paidSaving,
            RemainEmi: remainEmi,
            RemainPrincipal: remainPrincipal,
            RemainInterest: remainInterest,
            RemainSaving: remainSaving);
    }

    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
