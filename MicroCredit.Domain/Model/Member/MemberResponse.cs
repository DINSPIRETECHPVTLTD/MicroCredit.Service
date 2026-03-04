namespace MicroCredit.Domain.Model.Member;

public class MemberResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
