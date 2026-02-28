using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("LedgerTransactions")]
public class LedgerTransaction
{
    [Key]
    public int Id { get; private set; }

    public int? PaidFromUserId { get; private set; }

    public int? PaidToUserId { get; private set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    [Required]
    public DateTime PaymentDate { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    [Required]
    [StringLength(50)]
    public string TransactionType { get; private set; } = string.Empty;

    public int? ReferenceId { get; private set; }

    [StringLength(500)]
    public string? Comments { get; private set; }

    // Navigation
    [ForeignKey("PaidFromUserId")]
    public virtual User? FromUser { get; private set; }

    [ForeignKey("PaidToUserId")]
    public virtual User? ToUser { get; private set; }

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    private LedgerTransaction() { } // EF

    public LedgerTransaction(int? paidFromUserId, int? paidToUserId, decimal amount, DateTime paymentDate,
        int createdBy, string transactionType, int? referenceId = null, string? comments = null)
    {
        PaidFromUserId = paidFromUserId;
        PaidToUserId = paidToUserId;
        Amount = amount;
        PaymentDate = paymentDate;
        CreatedBy = createdBy;
        CreatedDate = DateTime.UtcNow;
        TransactionType = transactionType;
        ReferenceId = referenceId;
        Comments = comments;
    }
}
