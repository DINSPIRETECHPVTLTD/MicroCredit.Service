namespace MicroCredit.Application.Model.Master;

public class PaymentTermResponse
{
    public int Id { get; set; }
    public string PaymentTerm { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public int NoOfTerms { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal RateOfInterest { get; set; }
    public decimal InsuranceFee { get; set; }
}
