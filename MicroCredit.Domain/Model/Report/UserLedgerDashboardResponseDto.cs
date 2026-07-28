namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Logged-in user's ledger balance and transaction history for the branch dashboard report.
/// </summary>
public class UserLedgerDashboardResponseDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public UserLedgerDashboardSummaryDto Summary { get; set; } = new();
    public List<UserLedgerTransactionRowDto> Transactions { get; set; } = new();
}

public class UserLedgerDashboardSummaryDto
{
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public int TransactionCount { get; set; }
}

public class UserLedgerTransactionRowDto
{
    public int Id { get; set; }
    public int? PaidFromUserId { get; set; }
    public int? PaidToUserId { get; set; }
    public string? PaidFromUserName { get; set; }
    public string? PaidToUserName { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Comments { get; set; }
    /// <summary>Credit or Debit relative to the logged-in user.</summary>
    public string Direction { get; set; } = string.Empty;
}
