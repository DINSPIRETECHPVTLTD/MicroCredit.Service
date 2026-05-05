using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class InsuranceClaimFinancialSummaryRepository : IInsuranceClaimFinancialSummaryRepository
{
    private readonly MicroCreditDbContext _context;

    public InsuranceClaimFinancialSummaryRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task AccumulateLoanCreationTotalsAsync(
        decimal insuranceFee,
        decimal processingFee,
        CancellationToken cancellationToken = default)
    {
        var summary = await _context.InsuranceClaimFinancialSummaries
            .OrderBy(s => s.SummaryId)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
        {
            summary = new InsuranceClaimFinancialSummary(
                totalInsuranceAmount: insuranceFee,
                totalClaimedAmount: 0m,
                totalProcessingFee: processingFee,
                totalJoiningFee: 0m,
                totalExpenseAmount: 0m);
            await _context.InsuranceClaimFinancialSummaries.AddAsync(summary, cancellationToken);
        }
        else
        {
            summary.AddLoanInsuranceAndProcessingFee(insuranceFee, processingFee);
        }
    }

    public async Task<(decimal InsuranceAmountBeforeClaim, decimal RemainingInsuranceAmount)> ApplyInsuranceClaimAsync(
        decimal claimAmount,
        CancellationToken cancellationToken = default)
    {
        var summary = await _context.InsuranceClaimFinancialSummaries
            .OrderBy(s => s.SummaryId)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
        {
            summary = new InsuranceClaimFinancialSummary(
                totalInsuranceAmount: 0m,
                totalClaimedAmount: 0m,
                totalProcessingFee: 0m,
                totalJoiningFee: 0m,
                totalExpenseAmount: 0m);
            await _context.InsuranceClaimFinancialSummaries.AddAsync(summary, cancellationToken);
        }

        var insuranceAmountBeforeClaim = summary.TotalInsuranceAmount - summary.TotalClaimedAmount;
        var remainingInsuranceAmount = insuranceAmountBeforeClaim - claimAmount;
        summary.RecordInsuranceClaim(claimAmount);

        return (insuranceAmountBeforeClaim, remainingInsuranceAmount);
    }

    public async Task AccumulateJoiningFeeAsync(
        decimal joiningFeeAmount,
        CancellationToken cancellationToken = default)
    {
        if (joiningFeeAmount <= 0m)
            return;

        var summary = await _context.InsuranceClaimFinancialSummaries
            .OrderBy(s => s.SummaryId)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
        {
            summary = new InsuranceClaimFinancialSummary(
                totalInsuranceAmount: 0m,
                totalClaimedAmount: 0m,
                totalProcessingFee: 0m,
                totalJoiningFee: joiningFeeAmount,
                totalExpenseAmount: 0m);
            await _context.InsuranceClaimFinancialSummaries.AddAsync(summary, cancellationToken);
        }
        else
        {
            summary.AddJoiningFee(joiningFeeAmount);
        }
    }
}
