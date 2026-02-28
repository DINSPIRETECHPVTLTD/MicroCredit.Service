using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("MemberMembershipFees")]
public class MemberMembershipFee
{
    [Key]
    public int Id { get; private set; }

    [Required]
    public int MemberId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    public DateTime? PaidDate { get; private set; }

    public int? CollectedBy { get; private set; }

    [StringLength(50)]
    public string? PaymentMode { get; private set; }

    [StringLength(500)]
    public string? Comments { get; private set; }

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

    [ForeignKey("CollectedBy")]
    public virtual User? CollectedByUser { get; private set; }

    private MemberMembershipFee() { } // EF

    public MemberMembershipFee(int memberId, decimal amount, int createdBy, int? collectedBy = null,
        DateTime? paidDate = null, string? paymentMode = null, string? comments = null)
    {
        MemberId = memberId;
        Amount = amount;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        CollectedBy = collectedBy;
        PaidDate = paidDate;
        PaymentMode = paymentMode;
        Comments = comments;
    }

    public void RecordPayment(DateTime paidDate, int collectedBy, string? paymentMode = null, string? comments = null)
    {
        PaidDate = paidDate;
        CollectedBy = collectedBy;
        PaymentMode = paymentMode;
        Comments = comments;
    }

    public void MarkDeleted(int modifiedBy)
    {
        IsDeleted = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
