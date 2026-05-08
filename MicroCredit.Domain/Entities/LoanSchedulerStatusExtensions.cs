namespace MicroCredit.Domain.Entities;

public static class LoanSchedulerStatusExtensions
{
    public static string ToDbValue(this LoanSchedulerStatus status) => status switch
    {
        LoanSchedulerStatus.NotPaid => "Not Paid",
        LoanSchedulerStatus.Paid => "Paid",
        LoanSchedulerStatus.Partial => "Partial",
        LoanSchedulerStatus.Claimed => "Claimed",
        LoanSchedulerStatus.Overdue => "Overdue",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported loan scheduler status."),
    };

    public static LoanSchedulerStatus FromDbValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LoanSchedulerStatus.NotPaid;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "not paid" => LoanSchedulerStatus.NotPaid,
            "paid" => LoanSchedulerStatus.Paid,
            "partial" => LoanSchedulerStatus.Partial,
            "partial paid" => LoanSchedulerStatus.Partial,
            "claimed" => LoanSchedulerStatus.Claimed,
            "overdue" => LoanSchedulerStatus.Overdue,
            _ => throw new InvalidOperationException($"Unknown loan scheduler status '{value}'."),
        };
    }
}
