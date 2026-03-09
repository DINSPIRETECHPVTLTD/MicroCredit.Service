namespace MicroCredit.Domain.Model.Fund;

public class InvestmentResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? InvestmentDate { get; set; }
    public int CreatedById { get; set; }
    public DateTime? CreatedDate { get; set; }

}
