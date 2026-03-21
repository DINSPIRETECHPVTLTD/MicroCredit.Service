using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;

namespace MicroCredit.Application.Services;

/// <summary>
/// Common logic for recording a ledger transaction and updating the Ledgers table.
/// Used by both Add Investment and Add Expense flows.
/// </summary>
public class LedgerRecordService : ILedgerRecordService
{
    private readonly IUnitOfWork _unitOfWork;

    public LedgerRecordService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LedgerTransaction> CreateTransactionAsync(
    int? paidFromUserId,
    int? paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    string transactionType,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Transaction amount must be greater than zero");

        if (paidFromUserId == null && paidToUserId == null)
            throw new InvalidOperationException("Either PaidFromUserId or PaidToUserId must be specified");

        var ledgerTransaction = new LedgerTransaction(
                paidFromUserId: paidFromUserId,
                paidToUserId: paidToUserId,
                amount: amount,
                paymentDate: paymentDate,
                createdBy: createdBy,
                createdDate: createdDate,
                transactionType: transactionType,
                referenceId: referenceId,
                comments: comments);

        await _unitOfWork.LedgerTransaction.AddAsync(ledgerTransaction, cancellationToken);

        if (paidFromUserId.HasValue)
            await UpdateLedgerBalanceAsync(paidFromUserId.Value, -amount, cancellationToken);

        if (paidToUserId.HasValue)
            await UpdateLedgerBalanceAsync(paidToUserId.Value, amount, cancellationToken);

        await _unitOfWork.CompleteAsync();

        return ledgerTransaction;
    }

    public async Task<LedgerTransaction> RecordInvestmentAsync(
    int paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    string transactionType,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default)
    {
        return await CreateTransactionAsync(
            null,
            paidToUserId,
            amount,
            paymentDate,
            createdBy,
            createdDate,
            transactionType,
            referenceId,
            comments,
            cancellationToken);
    }


    public async Task<LedgerTransaction> RecordExpenseAsync(
    int paidFromUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default)
    {
        var ledger = await _unitOfWork.LedgerBalances.GetByUserIdAsync(paidFromUserId, cancellationToken);

        if (ledger.Amount < amount)
            throw new InvalidOperationException("Insufficient balance");

        return await CreateTransactionAsync(
            paidFromUserId,
            null,
            amount,
            paymentDate,
            createdBy,
            createdDate,
            "Expense",
            referenceId,
            comments,
            cancellationToken);
    }

    public async Task<LedgerTransaction> RecordTransferAsync(
    int paidFromUserId,
    int paidToUserId,
    decimal amount,
    DateTime paymentDate,
    int createdBy,
    DateTime createdDate,
    int? referenceId = null,
    string? comments = null,
    CancellationToken cancellationToken = default)
    {
        var ledger = await _unitOfWork.LedgerBalances.GetByUserIdAsync(paidFromUserId, cancellationToken); ;

        if (ledger.Amount < amount)
            throw new InvalidOperationException("Insufficient balance");

        return await CreateTransactionAsync(
            paidFromUserId,
            paidToUserId,
            amount,
            paymentDate,
            createdBy,
            createdDate,
            "Fund Transfer",
            referenceId,
            comments,
            cancellationToken);
    }

    public async Task UpdateLedgerBalanceAsync(
    int userId,
    decimal balanceChange,
    CancellationToken cancellationToken)
    {
        var ledger = await _unitOfWork.LedgerBalances.GetByUserIdAsync(userId, cancellationToken);

        if (ledger == null)
        {
            ledger = new Ledger(userId, 0);
            await _unitOfWork.LedgerBalances.AddAsync(ledger, cancellationToken);
        }

        ledger.UpdateAmount(ledger.Amount + balanceChange);

        if (ledger.Amount < 0)
            throw new InvalidOperationException("Transaction would result in negative balance");
    }

    public async Task<LedgerTransaction> RecordWithdrawalAsync(
        int paidFromUserId,
        decimal amount,
        DateTime paymentDate,
        int createdBy,
        DateTime createdDate,
        int? referenceId = null,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        var ledger = await _unitOfWork.LedgerBalances.GetByUserIdAsync(paidFromUserId, cancellationToken); ;

        if (ledger.Amount < amount)
            throw new InvalidOperationException("Insufficient balance");

        return await CreateTransactionAsync(
            paidFromUserId,
            null,
            amount,
            paymentDate,
            createdBy,
            createdDate,
            "Loan disbursement",
            referenceId,
            comments,
            cancellationToken);

    }

    public async Task<LedgerTransaction> RecordDepositAsync(
        int paidToUserId,
        decimal amount,
        DateTime paymentDate,
        int createdBy,
        DateTime createdDate,
        string transactionType,
        int? referenceId = null,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateTransactionAsync(
            null,
            paidToUserId,
            amount,
            paymentDate,
            createdBy,
            createdDate,
            transactionType,
            referenceId,
            comments,
            cancellationToken);
    }
}
