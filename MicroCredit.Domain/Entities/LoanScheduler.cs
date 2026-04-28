using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("LoanSchedulers")]
public class LoanScheduler
{
    [Key]
    public int LoanSchedulerId { get; private set; }

    [Required]
    public int LoanId { get; private set; }

    [Required]
    public DateTime ScheduleDate { get; private set; }

    public DateTime? PaymentDate { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualEmiAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualPrincipalAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualInterestAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaymentAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SavingAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; private set; }

    [Required]
    public int InstallmentNo { get; private set; }

    [Required]
    [StringLength(20)]
    public string Status { get; private set; } = "Not Paid"; // Paid, Partial, Not Paid

    [StringLength(50)]
    public string? PaymentMode { get; private set; }

    public int? CollectedBy { get; private set; }

    [StringLength(500)]
    public string? Comments { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    // Navigation
    [ForeignKey("LoanId")]
    public virtual Loan Loan { get; private set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("CollectedBy")]
    public virtual User? CollectedByUser { get; private set; }

    private LoanScheduler() { } // EF

    public LoanScheduler(int loanId, DateTime scheduleDate, decimal paymentAmount, decimal principalAmount,
        decimal interestAmount, int installmentNo, int createdBy, decimal actualEmiAmount = 0, decimal actualPrincipalAmount = 0,
        decimal actualInterestAmount = 0, decimal? savingAmount = null)
    {
        LoanId = loanId;
        ScheduleDate = scheduleDate;
        PaymentAmount = paymentAmount;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        InstallmentNo = installmentNo;
        CreatedBy = createdBy;
        CreatedDate = DateTime.UtcNow;
        Status = "Not Paid";
        ActualEmiAmount = actualEmiAmount;
        ActualPrincipalAmount = actualPrincipalAmount;
        ActualInterestAmount = actualInterestAmount;
        SavingAmount = savingAmount;
    }

    public void RecordPayment(decimal actualEmiAmount, decimal actualPrincipalAmount, decimal actualInterestAmount,
        int collectedBy, string? paymentMode = null, decimal? savingAmount = null, string? comments = null)
    {
        PaymentDate = DateTime.UtcNow;
        ActualEmiAmount = actualEmiAmount;
        ActualPrincipalAmount = actualPrincipalAmount;
        ActualInterestAmount = actualInterestAmount;
        CollectedBy = collectedBy;
        PaymentMode = paymentMode;
        SavingAmount = savingAmount;
        Comments = comments;
        Status = "Paid";
    }

    public void RecordPartialPayment(decimal amountPaid, decimal actualPrincipalAmount, decimal actualInterestAmount,
        int collectedBy, string? paymentMode = null, string? comments = null)
    {
        PaymentDate = DateTime.UtcNow;
        ActualEmiAmount = amountPaid;
        ActualPrincipalAmount = actualPrincipalAmount;
        ActualInterestAmount = actualInterestAmount;
        CollectedBy = collectedBy;
        PaymentMode = paymentMode;
        Comments = comments;
        Status = "Partial";
    }

    public void AdjustAmounts(decimal newPrincipal, decimal newInterest)
    {
        ActualPrincipalAmount = Math.Round(newPrincipal, 2);
        ActualInterestAmount = Math.Round(newInterest, 2);
        ActualEmiAmount = ActualPrincipalAmount + ActualInterestAmount;
    }

    public void MarkClaimed(int collectedBy)
    {
        PaymentDate = DateTime.UtcNow;
        Status = "Claimed";
        CollectedBy = collectedBy;
    }
}
