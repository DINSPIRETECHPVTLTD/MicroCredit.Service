using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Insurance_Claim_Financial_Summary")]
public class InsuranceClaimFinancialSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SummaryId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalInsuranceAmount { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalClaimedAmount { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalProcessingFee { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalJoiningFee { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalExpenseAmount { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    private InsuranceClaimFinancialSummary()
    {
    }

    public InsuranceClaimFinancialSummary(
        decimal totalInsuranceAmount,
        decimal totalClaimedAmount,
        decimal totalProcessingFee,
        decimal totalJoiningFee,
        decimal totalExpenseAmount)
    {
        TotalInsuranceAmount = totalInsuranceAmount;
        TotalClaimedAmount = totalClaimedAmount;
        TotalProcessingFee = totalProcessingFee;
        TotalJoiningFee = totalJoiningFee;
        TotalExpenseAmount = totalExpenseAmount;
    }

    public void AddLoanInsuranceAndProcessingFee(decimal insuranceFee, decimal processingFee)
    {
        TotalInsuranceAmount += insuranceFee;
        TotalProcessingFee += processingFee;
    }

    public void AddJoiningFee(decimal joiningFee)
    {
        if (joiningFee <= 0m)
            return;
        TotalJoiningFee += joiningFee;
    }

    /// <summary>
    /// Records a claim by accumulating into TotalClaimedAmount.
    /// No pool sufficiency validation is applied as per current business rule.
    /// </summary>
    public void RecordInsuranceClaim(decimal claimAmount)
    {
        if (claimAmount <= 0m)
            throw new InvalidOperationException("Claim amount must be greater than zero.");

        TotalClaimedAmount += claimAmount;
    }
}
