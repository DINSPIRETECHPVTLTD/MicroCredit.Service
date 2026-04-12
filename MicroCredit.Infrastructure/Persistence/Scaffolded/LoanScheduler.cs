using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class LoanScheduler
{
    public int LoanSchedulerId { get; set; }

    public int LoanId { get; set; }

    public DateTime ScheduleDate { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal ActualEmiAmount { get; set; }

    public decimal ActualPrincipalAmount { get; set; }

    public decimal ActualInterestAmount { get; set; }

    public decimal PaymentAmount { get; set; }

    public decimal? SavingAmount { get; set; }

    public decimal PrincipalAmount { get; set; }

    public decimal InterestAmount { get; set; }

    public int InstallmentNo { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentMode { get; set; }

    public int? CollectedBy { get; set; }

    public string? Comments { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User? CollectedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Loan Loan { get; set; } = null!;
}
