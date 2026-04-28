using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Service;

/// <summary>
/// Records a ledger transaction and optionally updates the ledger balance for a user.
/// Used by both Investment and Expense flows.
/// </summary>
public interface ILedgerRecordService
{
    /// <summary>
    /// Adds the transaction to LedgerTransactions and, when ledgerUserId and balanceDelta are provided,
    /// gets or creates the Ledger for that user and updates the balance by balanceDelta.
    /// Caller is responsible for calling UnitOfWork.CompleteAsync() after this.
    /// </summary>
    public Task<LedgerTransaction> CreateTransactionAsync(
    int? paidFromUserId,
    int? paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    string transactionType,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default);

    public Task<LedgerTransaction> RecordInvestmentAsync(
    int paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    string transactionType = "Investment",
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default);

    public Task<LedgerTransaction> RecordExpenseAsync(
        int paidFromUserId,
        decimal amount,
        DateTime paymentDate,
        int createdBy,
        DateTime createdDate,
        int? referenceId = null,
        string? comments = null,
        CancellationToken cancellationToken = default);

    public Task<LedgerTransaction> RecordTransferAsync(
    int paidFromUserId,
    int paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default);

    public Task UpdateLedgerBalanceAsync(
       int userId,
       decimal balanceChange,
       CancellationToken cancellationToken);

    public Task UpdateLedgerInsuranceAmountAsync(
       int userId,
       decimal insuranceAmountChange,
       CancellationToken cancellationToken);

    public Task<LedgerTransaction> RecordWithdrawalAsync(
        int paidFromUserId,
        decimal amount,
        DateTime paymentDate,
        int createdBy,
        DateTime createdDate,
        int? referenceId = null,
        string? comments = null,
        CancellationToken cancellationToken = default);

    public Task<LedgerTransaction> RecordDepositAsync(
        int paidToUserId,
        decimal amount,
        DateTime paymentDate,
        int createdBy,
        DateTime createdDate,
        string transactionType,
        int? referenceId = null,
        string? comments = null,
        CancellationToken cancellationToken = default);

}