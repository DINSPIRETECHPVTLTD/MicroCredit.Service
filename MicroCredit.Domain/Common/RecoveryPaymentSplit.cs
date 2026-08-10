namespace MicroCredit.Domain.Common;

/// <summary>
/// Ports React/TypeScript recoveryPostingCalculations.ts round2 + calculatePaymentSplitFromSchedule.
/// Must stay behavior-compatible with the existing frontend source of truth.
/// </summary>
public static class RecoveryPaymentSplit
{
    /// <summary>Same as TS: Math.round(n * 100) / 100 for finite numbers.</summary>
    public static decimal Round2(decimal n)
    {
        // Match JS Math.round on the scaled value (half away from zero for positive values).
        var scaled = n * 100m;
        var rounded = scaled >= 0
            ? Math.Floor(scaled + 0.5m)
            : Math.Ceiling(scaled - 0.5m);
        return rounded / 100m;
    }

    public static (decimal PrincipalAmount, decimal InterestAmount) CalculatePaymentSplitFromSchedule(
        decimal actualEmiAmount,
        decimal? actualPrincipalAmount,
        decimal? actualInterestAmount,
        decimal payment,
        decimal? principalPercentage = null)
    {
        if (payment <= 0)
            return (0m, 0m);

        var total = actualEmiAmount > 0 ? actualEmiAmount : 0m;
        var principal = actualPrincipalAmount ?? 0m;
        var interest = actualInterestAmount ?? 0m;

        var hasPrincipalAndInterest = actualPrincipalAmount.HasValue && actualInterestAmount.HasValue;
        var totalFromPI = hasPrincipalAndInterest ? principal + interest : 0m;

        if (totalFromPI > 0)
        {
            var principalRatio = principal / totalFromPI;
            var principalAmount = Round2(payment * principalRatio);
            var interestAmount = Round2(payment - principalAmount);
            return (principalAmount, interestAmount);
        }

        if (total > 0)
        {
            var principalRatio = principal / total;
            var principalAmount = Round2(payment * principalRatio);
            var interestAmount = Round2(payment - principalAmount);
            return (principalAmount, interestAmount);
        }

        var pPct = principalPercentage ?? 0m;
        var fallbackPrincipal = Round2((payment * pPct) / 100m);
        var fallbackInterest = Round2(payment - fallbackPrincipal);
        return (fallbackPrincipal, fallbackInterest);
    }
}
