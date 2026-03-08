using System.ComponentModel.DataAnnotations;

namespace MicroCredit.Domain.Model.Fund;

public class ExpenseResponse
{
    public int Id { get; set; }
    public int? PaidFromUserId { get; set; }
    public int? PaidToUserId { get; set; }

    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public required string TransactionType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Comments { get; set; }


}
