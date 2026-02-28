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
    public int Id { get; private set; }
    public string LookupKey { get; private set; } = null!;

    public string LookupCode { get; private set; } = null!;

    public string LookupValue { get; private set; } = null!;

    public decimal? NumericValue { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedOn { get; private set; }
    public string? CreatedBy { get; private set; }

    public DateTime? UpdatedOn { get; private set; }
    public string? UpdatedBy { get; private set; }

    private MasterLookup() { } // EF – lookup/legacy table
}
