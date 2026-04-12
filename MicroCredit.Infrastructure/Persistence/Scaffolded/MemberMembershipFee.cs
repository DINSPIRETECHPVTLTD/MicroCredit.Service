using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class MemberMembershipFee
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaidDate { get; set; }

    public int? CollectedBy { get; set; }

    public string? PaymentMode { get; set; }

    public string? Comments { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User? CollectedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }
}
