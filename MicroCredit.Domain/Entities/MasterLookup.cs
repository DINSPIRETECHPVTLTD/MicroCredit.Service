using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

public static class LookupKeys
{
    public const string LoanTerm = "LOAN_TERM";
    public const string PaymentType = "PAYMENT_TYPE";
    public const string Relationship = "RELATIONSHIP";
    public const string State = "STATE";
    public const string PaymentMode = "PAYMENTMODE";
}

[Table("MasterLookups")]
public class MasterLookup
{
    public int Id { get; set; }
    public string LookupKey { get; set; } = null!;

    public string LookupCode { get; set; } = null!;

    public string LookupValue { get; set; } = null!;

    public decimal? NumericValue { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
}
