using System.ComponentModel.DataAnnotations;

namespace MicroCredit.Domain.Model.Fund;

public class CreateInvestmentRequest
{
    public int UserId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public DateTime InvestmentDate { get; set; }

    public DateTime CreatedDate { get; set; }
}
