namespace MicroCredit.Domain.Model.Member;

public class MemberRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CenterId { get; set; }
    public int POCId { get; set; }
    public string GuardianFirstName { get; set; } = string.Empty;
    public string GuardianSurName { get; set; } = string.Empty;
    public string GuardianPhone { get; set; } = string.Empty;
    public int Age { get; set; }
    public int GuardianAge { get; set; }
}
