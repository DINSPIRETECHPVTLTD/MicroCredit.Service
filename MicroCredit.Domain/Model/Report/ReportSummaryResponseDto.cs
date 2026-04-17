namespace MicroCredit.Domain.Model.Report;

public class ReportSummaryResponseDto
{
    public decimal TotalOwnerAmount { get; set; }
    public decimal TotalInvestorAmount { get; set; }
    public decimal TotalInsuranceAmount { get; set; }
    public decimal TotalProcessingFee { get; set; }
    public decimal ReceivedPrinciple { get; set; }
    public decimal ReceivedInterest { get; set; }
    public decimal OutstandingPrinciple { get; set; }
    public decimal InterestAccured { get; set; }
    public decimal TotalJoiningFee { get; set; }
    public decimal TotalLedgerExpenseAmount { get; set; }
}
