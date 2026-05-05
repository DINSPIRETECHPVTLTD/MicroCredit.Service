namespace MicroCredit.Domain.Interfaces.Repository;

public interface IInsuranceClaimFinancialSummaryRepository
{
    /// <summary>
    /// Adds running totals for a new loan. Creates the singleton summary row when none exists.
    /// </summary>
    Task AccumulateLoanCreationTotalsAsync(
        decimal insuranceFee,
        decimal processingFee,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates pool balance, increments total claimed on the summary row, and returns amounts for the API response.
    /// </summary>
    Task<(decimal InsuranceAmountBeforeClaim, decimal RemainingInsuranceAmount)> ApplyInsuranceClaimAsync(
        decimal claimAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a member joining fee into TotalJoiningFee. Creates the singleton summary row when none exists.
    /// </summary>
    Task AccumulateJoiningFeeAsync(
        decimal joiningFeeAmount,
        CancellationToken cancellationToken = default);
}
