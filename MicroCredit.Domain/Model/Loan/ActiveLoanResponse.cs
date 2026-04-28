namespace MicroCredit.Domain.Model.Loan;

public class ActiveLoanResponse
{
    public int LoanId { get; set; }
    public int MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal LoanTotalAmount { get; set; }
    public string NoOfTerms { get; set; } = string.Empty;
    public decimal TotalAmountPaid { get; set; }
    public decimal SchedulerTotalAmount { get; set; }
    public decimal RemainingBal { get; set; }
}
