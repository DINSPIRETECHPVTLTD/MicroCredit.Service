using System;
using System.Collections.Generic;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class PaymentTerm
{
    public int PaymentTermId { get; set; }

    public string PaymentTerm1 { get; set; } = null!;

    public string PaymentType { get; set; } = null!;

    public int NoOfTerms { get; set; }

    public decimal? ProcessingFee { get; set; }

    public decimal? RateOfInterest { get; set; }

    public decimal? InsuranceFee { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }
}
