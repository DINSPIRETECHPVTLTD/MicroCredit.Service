using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Ledger
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public virtual User User { get; set; } = null!;
}
