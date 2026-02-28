using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Loans")]
public class Loan
{
    [Key]
    public int Id { get; private set; }

    [Required]
    public int MemberId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LoanAmount { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcessingFee { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceFee { get; private set; }

    [Required]
    public bool IsSavingEnabled { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SavingAmount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; private set; }

    [StringLength(20)]
    public string Status { get; private set; } = "Active"; // Active, Closed, Defaulted

    public DateTime? DisbursementDate { get; private set; }

    public DateTime? ClosureDate { get; private set; }

    public DateTime? CollectionStartDate { get; private set; }

    [Required]
    [StringLength(50)]
    public string CollectionTerm { get; private set; } = string.Empty;

    [Required]
    public int NoOfTerms { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; }

    // Navigation
    [ForeignKey("MemberId")]
    public virtual Member Member { get; private set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    public virtual ICollection<LoanScheduler>? LoanSchedulers { get; private set; }

    private Loan() { } // EF

    public Loan(int memberId, decimal loanAmount, decimal interestAmount, decimal processingFee, decimal insuranceFee,
        bool isSavingEnabled, decimal savingAmount, decimal totalAmount, string collectionTerm, int noOfTerms, int createdBy)
    {
        MemberId = memberId;
        LoanAmount = loanAmount;
        InterestAmount = interestAmount;
        ProcessingFee = processingFee;
        InsuranceFee = insuranceFee;
        IsSavingEnabled = isSavingEnabled;
        SavingAmount = savingAmount;
        TotalAmount = totalAmount;
        Status = "Active";
        CollectionTerm = collectionTerm;
        NoOfTerms = noOfTerms;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void Disburse(DateTime? collectionStartDate = null)
    {
        DisbursementDate = DateTime.UtcNow;
        CollectionStartDate = collectionStartDate ?? DateTime.UtcNow;
    }

    public void CloseLoan()
    {
        Status = "Closed";
        ClosureDate = DateTime.UtcNow;
    }

    public void MarkDefaulted()
    {
        Status = "Defaulted";
    }

    public void MarkDeleted(int modifiedBy)
    {
        IsDeleted = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
