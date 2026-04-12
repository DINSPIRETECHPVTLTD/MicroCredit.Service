using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Center
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int BranchId { get; set; }

    public string? CenterAddress { get; set; }

    public string? City { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<Poc> Pocs { get; set; } = new List<Poc>();
}
