using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Member
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? AltPhone { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public string? Aadhaar { get; set; }

    public string? Occupation { get; set; }

    public string? Relationship { get; set; }

    public DateOnly? Dob { get; set; }

    public int Age { get; set; }

    public string GuardianFirstName { get; set; } = null!;

    public string? GuardianMiddleName { get; set; }

    public string GuardianLastName { get; set; } = null!;

    public string GuardianPhone { get; set; } = null!;

    public DateOnly? GuardianDob { get; set; }

    public int GuardianAge { get; set; }

    public int CenterId { get; set; }

    public int Pocid { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public virtual ICollection<MemberMembershipFee> MemberMembershipFees { get; set; } = new List<MemberMembershipFee>();

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Poc Poc { get; set; } = null!;
}
