namespace MicroCredit.Domain.Model.Loan;

public class CloseLoanResponse
{
    public int LoanId { get; set; }
    public bool IsClosed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ClosureDate { get; set; }
}
