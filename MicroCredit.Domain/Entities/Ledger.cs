using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Ledgers")]
public class Ledger
{
    [Key]
    public int Id { get; private set; }

    [Required]
    public int UserId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? InsuranceAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ClaimedAmount { get; private set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; private set; } = null!;

    private Ledger() { } // EF

    public Ledger(int userId, decimal amount)
    {
        UserId = userId;
        Amount = amount;
    }

    public void UpdateAmount(decimal amount)
    {
        Amount = amount;
    }

    public void AddInsuranceAmount(decimal amount)
    {
        InsuranceAmount = (InsuranceAmount ?? 0m) + amount;
    }

    public void ApplyClaimAmounts(decimal remainingInsuranceAmount, decimal claimedAmount)
    {
        InsuranceAmount = remainingInsuranceAmount;
        ClaimedAmount = claimedAmount;
    }
}
