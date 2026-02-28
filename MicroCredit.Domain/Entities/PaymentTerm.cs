using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("PaymentTerms")]
public class PaymentTerm
{
    [Column("PaymentTermID")]
    public int PaymentTermId { get; private set; }

    [Column("PaymentTerm")]
    public string PaymentTermName { get; private set; } = null!;

    [Column("PaymentType")]
    public string PaymentType { get; private set; } = string.Empty;

    [Column("NoOfTerms")]
    public int NoOfTerms { get; private set; }

    [Column("ProcessingFee")]
    public decimal? ProcessingFee { get; private set; }

    [Column("RateOfInterest")]
    public decimal? RateOfInterest { get; private set; }

    [Column("InsuranceFee")]
    public decimal? InsuranceFee { get; private set; }

    [Column("CreatedBy")]
    public int CreatedBy { get; private set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; private set; }

    [Column("ModifiedBy")]
    public int? ModifiedBy { get; private set; }

    [Column("ModifiedAt")]
    public DateTime? ModifiedAt { get; private set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; private set; }

    private PaymentTerm() { } // EF – lookup/legacy table
}
