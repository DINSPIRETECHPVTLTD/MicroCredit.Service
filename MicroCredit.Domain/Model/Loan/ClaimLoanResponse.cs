namespace MicroCredit.Domain.Model.Loan;

public class ClaimLoanResponse
{
    public int LoanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPendingEmiAmount { get; set; }
    public decimal InsuranceAmountBeforeClaim { get; set; }
    public decimal RemainingInsuranceAmount { get; set; }
    public decimal ClaimedAmount { get; set; }
    public int UpdatedSchedulersCount { get; set; }
}
