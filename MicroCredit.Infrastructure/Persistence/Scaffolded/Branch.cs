using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Branch
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? ZipCode { get; set; }

    public string? PhoneNumber { get; set; }

    public int OrgId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Center> Centers { get; set; } = new List<Center>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Organization Org { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
