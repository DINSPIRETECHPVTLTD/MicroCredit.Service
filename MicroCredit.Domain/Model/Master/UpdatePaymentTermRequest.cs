namespace MicroCredit.Domain.Model.PaymentTerm;

public class UpdatePaymentTermRequest
{
    public int PaymentTermId { get; set; }
    public string PaymentTermName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public int NoOfTerms { get; set; }
    public decimal? ProcessingFee { get; set; }
    public decimal? RateOfInterest { get; set; }
    public decimal? InsuranceFee { get; set; }
}