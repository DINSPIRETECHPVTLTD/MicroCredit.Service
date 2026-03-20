using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("Members")]
public class Member
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; private set; } = string.Empty;

    [StringLength(100)]
    public string? MiddleName { get; private set; }

    [Required]
    [StringLength(100)]
    public string LastName { get; private set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; private set; } = string.Empty;

    [StringLength(20)]
    public string? AltPhone { get; private set; }

    [StringLength(200)]
    public string? Address1 { get; private set; }

    [StringLength(200)]
    public string? Address2 { get; private set; }

    [StringLength(100)]
    public string? City { get; private set; }

    [StringLength(100)]
    public string? State { get; private set; }

    [StringLength(20)]
    public string? ZipCode { get; private set; }

    [StringLength(20)]
    public string? Aadhaar { get; private set; }

    [StringLength(100)]
    public string? Occupation { get; private set; }

    [StringLength(100)]
    public string? Relationship { get; private set; }

    public DateOnly? DOB { get; private set; }

    [Required]
    public int Age { get; private set; }

    public string GuardianFirstName { get; private set; } = string.Empty;

    [StringLength(100)]
    public string? GuardianMiddleName { get; private set; }

    [Required]
    [StringLength(100)]
    public string GuardianLastName { get; private set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string GuardianPhone { get; private set; } = string.Empty;

    public DateOnly? GuardianDOB { get; private set; }

    [Required]
    public int GuardianAge { get; private set; }

    [Required]
    public int CenterId { get; private set; }

    [Required]
    public int POCId { get; private set; }

    [Required]
    public int CreatedBy { get; private set; }

    [Required]
    public DateTime CreatedAt { get; private set; }

    public int? ModifiedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    [Required]
    public bool IsDeleted { get; private set; }

    // Navigation
    [ForeignKey("CenterId")]
    public virtual Center Center { get; private set; } = null!;

    [ForeignKey("POCId")]
    public virtual POC POC { get; private set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; private set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; private set; }

    public virtual ICollection<MemberMembershipFee> MemberMembershipFees { get; private set; } = new List<MemberMembershipFee>();

    private Member() { } // EF

    public Member(string firstName, string lastName, string phoneNumber, int centerId, int pocId, int createdBy,
        string guardianFirstName, string guardianLastName, string guardianPhone, int age, int guardianAge,
        string? middleName = null, string? altPhone = null, string? address1 = null, string? address2 = null,
        string? city = null, string? state = null, string? zipCode = null, string? aadhaar = null,
        string? occupation = null, string? relationship = null, DateOnly? dob = null, string? guardianMiddleName = null,
        DateOnly? guardianDob = null)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CenterId = centerId;
        POCId = pocId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
        GuardianFirstName = guardianFirstName;
        GuardianLastName = guardianLastName;
        GuardianPhone = guardianPhone;
        Age = age;
        GuardianAge = guardianAge;
        MiddleName = middleName;
        AltPhone = altPhone;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        Aadhaar = aadhaar;
        Occupation = occupation;
        Relationship = relationship;
        DOB = dob;
        GuardianMiddleName = guardianMiddleName;
        GuardianDOB = guardianDob;
    }

    public void UpdateDetails(int centerId, int pocId, string firstName, string? middleName, string lastName, string phoneNumber, string? altPhone,
        string? address1, string? address2, string? city, string? state, string? zipCode, string? aadhaar,
        string? occupation, string? relationship, DateOnly? dob, int age, string guardianFirstName, string? guardianMiddleName,
        string guardianLastName, string guardianPhone, DateOnly? guardianDob, int guardianAge, int modifiedBy)
    {
        CenterId = centerId;
        POCId = pocId;
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        AltPhone = altPhone;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
        Aadhaar = aadhaar;
        Occupation = occupation;
        Relationship = relationship;
        DOB = dob;
        Age = age;
        GuardianFirstName = guardianFirstName;
        GuardianMiddleName = guardianMiddleName;
        GuardianLastName = guardianLastName;
        GuardianPhone = guardianPhone;
        GuardianDOB = guardianDob;
        GuardianAge = guardianAge;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void MarkDeleted(int modifiedBy)
    {
        IsDeleted = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
