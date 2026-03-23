namespace MicroCredit.Domain.Model.RecoveryPosting;

/// <summary>
/// Filters for recovery posting loan scheduler listing.
/// </summary>
public class RecoveryPostingSchedulersRequest
{
    /// <summary>Schedule date to match (date portion is used).</summary>
    public DateTime ScheduleDate { get; set; }

    public int? CenterId { get; set; }

    public int? POCId { get; set; }
}

/// <summary>One row in a recovery posting batch.</summary>
public class RecoveryPostingPostLine
{
    public int LoanSchedulerId { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    /// <summary>Required. How the payment was collected (e.g. cash, UPI).</summary>
    public string? PaymentMode { get; set; }
    /// <summary>Required. Expected posting outcome: &quot;Paid&quot; (full EMI) or &quot;Partial Paid&quot; (partial).</summary>
    public string? Status { get; set; }
    /// <summary>Optional.</summary>
    public string? Comments { get; set; }
}

/// <summary>POST body for saving recovery payments to LoanSchedulers.</summary>
public class RecoveryPostingPostRequest
{
    public int CollectedBy { get; set; }
    public List<RecoveryPostingPostLine> Items { get; set; } = new();
}

public class RecoveryPostingPostResponse
{
    public int PostedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Read-only row used to validate recovery POST (no LoanScheduler entity dependency in application layer).</summary>
public class RecoveryPostingSchedulerSnapshot
{
    public int LoanSchedulerId { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNo { get; set; }
    public string Status { get; set; } = "";
    public decimal ActualEmiAmount { get; set; }
    public decimal ActualPrincipalAmount { get; set; }
    public decimal ActualInterestAmount { get; set; }
}
