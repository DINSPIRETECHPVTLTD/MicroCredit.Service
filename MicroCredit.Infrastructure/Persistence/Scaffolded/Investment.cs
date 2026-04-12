using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Investment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public DateTime InvestmentDate { get; set; }

    public int CreatedById { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User CreatedBy { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
