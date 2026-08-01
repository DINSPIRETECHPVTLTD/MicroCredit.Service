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

    /// <summary>0 = original EMI slot; 1+ = remainder after partial (display as InstallmentNo_SubInstallmentSequence).</summary>
    public int SubInstallmentSequence { get; private set; }

    /// <summary>When this row is a partial remainder, points to the row that was partially paid. Integer FK only.</summary>
    public int? ParentLoanSchedulerId { get; private set; }

    [Required]
    [StringLength(20)]
    public LoanSchedulerStatus Status { get; private set; } = LoanSchedulerStatus.NotPaid;

    [StringLength(50)]
    public string? PaymentMode { get; private set; }

    public int? CollectedBy { get; private set; }

    [StringLength(500)]
    public string? Comments { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    [ForeignKey("LoanId")]
    public virtual Loan Loan { get; private set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("CollectedBy")]
    public virtual User? CollectedByUser { get; private set; }

    [ForeignKey(nameof(ParentLoanSchedulerId))]
    public virtual LoanScheduler? ParentLoanScheduler { get; private set; }

    private LoanScheduler() { }

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
        Status = LoanSchedulerStatus.NotPaid;
        ActualEmiAmount = actualEmiAmount;
        ActualPrincipalAmount = actualPrincipalAmount;
        ActualInterestAmount = actualInterestAmount;
        SavingAmount = savingAmount;
        SubInstallmentSequence = 0;
    }

    /// <summary>
    /// Creates a Not Paid remainder row after a partial payment, using the same constructor as EMI generation.
    /// Supports recursive partials (6 → 6_1 → 6_2).
    /// </summary>
    public static LoanScheduler CreatePartialRemainder(
        int loanId,
        DateTime scheduleDate,
        int installmentNo,
        int subInstallmentSequence,
        int parentLoanSchedulerId,
        int createdBy,
        decimal actualEmiAmount,
        decimal actualPrincipalAmount,
        decimal actualInterestAmount,
        decimal? savingAmount)
    {
        var schedule = new LoanScheduler(
            loanId: loanId,
            scheduleDate: scheduleDate,
            paymentAmount: 0,
            principalAmount: 0,
            interestAmount: 0,
            installmentNo: installmentNo,
            createdBy: createdBy,
            actualEmiAmount: Math.Round(actualEmiAmount, 2),
            actualPrincipalAmount: Math.Round(actualPrincipalAmount, 2),
            actualInterestAmount: Math.Round(actualInterestAmount, 2),
            savingAmount: savingAmount);

        schedule.ParentLoanSchedulerId = parentLoanSchedulerId;
        schedule.SubInstallmentSequence = subInstallmentSequence;
        return schedule;
    }

    public string GetInstallmentLabel()
        => LoanSchedulerInstallmentHelper.FormatInstallmentLabel(InstallmentNo, SubInstallmentSequence);

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
        Status = LoanSchedulerStatus.Paid;
    }

    public void RecordPartialPayment(
        decimal amountPaid,
        decimal principalPaid,
        decimal interestPaid,
        decimal actualPrincipalAmount,
        decimal actualInterestAmount,
        int collectedBy,
        string? paymentMode = null,
        decimal? savingAmount = null,
        string? comments = null)
    {
        PaymentDate = DateTime.UtcNow;
        PaymentAmount = amountPaid;
        PrincipalAmount = principalPaid;
        InterestAmount = interestPaid;
        ActualEmiAmount = amountPaid;
        ActualPrincipalAmount = actualPrincipalAmount;
        ActualInterestAmount = actualInterestAmount;
        SavingAmount = savingAmount;
        CollectedBy = collectedBy;
        PaymentMode = paymentMode;
        Comments = comments;
        Status = LoanSchedulerStatus.Partial;
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
        Status = LoanSchedulerStatus.Claimed;
        CollectedBy = collectedBy;
    }

    public void ShiftScheduleDateByDays(int offsetDays)
    {
        ScheduleDate = ScheduleDate.AddDays(offsetDays);
    }
}
