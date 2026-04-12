using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class Loan
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public decimal LoanAmount { get; set; }

    public decimal InterestAmount { get; set; }

    public decimal ProcessingFee { get; set; }

    public decimal InsuranceFee { get; set; }

    public bool IsSavingEnabled { get; set; }

    public decimal SavingAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? DisbursementDate { get; set; }

    public DateTime? ClosureDate { get; set; }

    public DateTime? CollectionStartDate { get; set; }

    public string CollectionTerm { get; set; } = null!;

    public int NoOfTerms { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<LoanScheduler> LoanSchedulers { get; set; } = new List<LoanScheduler>();

    public virtual Member Member { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }
}
