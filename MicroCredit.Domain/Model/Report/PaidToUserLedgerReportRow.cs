namespace MicroCredit.Domain.Model.Report;

public class PaidToUserLedgerReportRow
{
    public int Id { get; set; }
    public string PaidToUserFullName { get; set; } = string.Empty;
    public int? PaidToUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
}
