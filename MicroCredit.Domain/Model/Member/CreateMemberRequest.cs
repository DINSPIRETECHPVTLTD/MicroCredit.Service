namespace MicroCredit.Domain.Model.Member;

public class CreateMemberRequest
{
    public int CenterId { get; set; }
    public int PocId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public DateTime? Dob { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Aadhaar { get; set; }
    public string? AltPhone { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string GuardianFirstName { get; set; } = string.Empty;
    public string? GuardianMiddleName { get; set; }
    public string GuardianLastName { get; set; } = string.Empty;
    public string? GuardianPhone { get; set; }
    public string? Relationship { get; set; }
    public DateTime? GuardianDob { get; set; }
    public int GuardianAge { get; set; }
}
