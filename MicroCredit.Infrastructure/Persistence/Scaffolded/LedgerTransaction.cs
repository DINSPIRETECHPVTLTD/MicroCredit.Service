using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class LedgerTransaction
{
    public int Id { get; set; }

    public int? PaidFromUserId { get; set; }

    public int? PaidToUserId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string TransactionType { get; set; } = null!;

    public int? ReferenceId { get; set; }

    public string? Comments { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? PaidFromUser { get; set; }

    public virtual User? PaidToUser { get; set; }
}
