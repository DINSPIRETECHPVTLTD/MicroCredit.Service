using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Poc
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

    public int CenterId { get; set; }

    public int CreatedBy { get; set; }

    public string? CollectionDay { get; set; }

    public string CollectionFrequency { get; set; } = null!;

    public int CollectionBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User CollectionByNavigation { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual User? ModifiedByNavigation { get; set; }
}
