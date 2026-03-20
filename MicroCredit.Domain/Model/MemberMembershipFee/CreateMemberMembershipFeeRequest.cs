namespace MicroCredit.Domain.Model.MemberMembershipFee;

public class CreateMemberMembershipFeeRequest
{
    public int MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaidDate { get; set; }
    public int? CollectedBy { get; set; }
    public string? PaymentMode { get; set; }
    public string? Comments { get; set; }
}

