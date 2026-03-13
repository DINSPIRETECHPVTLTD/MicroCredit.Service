using System.ComponentModel.DataAnnotations;

namespace MicroCredit.Domain.Model.Fund;

public class CreateExpenseRequest
{
    public int PaidFromUserId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }
    public DateTime CreatedDate { get; set; }
    [StringLength(500)]
    public string? Comments { get; set; }
}
