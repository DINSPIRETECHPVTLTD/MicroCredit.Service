namespace MicroCredit.Domain.Model.Member;

public class MemberResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AltPhone { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public int? CenterId { get; set; }
    public int? BranchId { get; set; }
    public string? Aadhaar { get; set; }
    public string? Occupation { get; set; }
    public string? Relationship { get; set; }
    public DateTime? DOB { get; set; }
    public int? Age { get; set; }
    public string? GuardianFirstName { get; set; }
    public string? GuardianMiddleName { get; set; }
    public string? GuardianLastName { get; set; }
    public string? GuardianPhone { get; set; }
    public DateTime? GuardianDOB { get; set; }
    public int? GuardianAge { get; set; }
    public int? POCId { get; set; }

    /// <summary>Center name from Center table (joined on CenterId). For Member grid display.</summary>
    public string? Center { get; set; }

    /// <summary>POC display name from POC table (joined on POCId). For Member grid display.</summary>
    public string? Poc { get; set; }

    public string GuardianName => string.Join(" ", new[]
    {
        GuardianFirstName,
        GuardianMiddleName,
        GuardianLastName
    }.Where(x => !string.IsNullOrWhiteSpace(x)));  
    public string Name => string.Join(" ", new[]
    {
        FirstName,
        MiddleName,
        LastName
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public string FullAddress => string.Join(", ", new[] { Address1, Address2, City, State, ZipCode }
                                       .Where(x => !string.IsNullOrWhiteSpace(x)));
}
