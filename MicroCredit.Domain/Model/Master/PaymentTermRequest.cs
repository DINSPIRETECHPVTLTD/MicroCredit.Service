namespace MicroCredit.Domain.Model.Master;

public class PaymentTermRequest
{
    public string PaymentTermName { get; set; } = null!;
    public string PaymentType { get; set; } = null!;
    public int NoOfTerms { get; set; }
    public decimal? ProcessingFee { get; set; }
    public decimal? RateOfInterest { get; set; }
    public decimal? InsuranceFee { get; set; }
}