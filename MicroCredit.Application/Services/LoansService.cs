using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Loan;
using MicroCredit.Application.Mappings.DomianEntity;
using Microsoft.AspNetCore.Http.HttpResults;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Application.Core;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Application.Services;

public class LoansService : ILoansService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerBalanceService _ledgerBalanceService;
    private readonly ILoanSchedulerService _loanSchedulerService;
    private readonly ILedgerRecordService _ledgerRecordService;

    public LoansService(IUnitOfWork unitOfWork, 
                        ILedgerBalanceService ledgerBalanceService, 
                        ILoanSchedulerService loanSchedulerService,
                        ILedgerRecordService ledgerRecordService)
    {
        _unitOfWork = unitOfWork;
        _ledgerBalanceService = ledgerBalanceService;
        _loanSchedulerService = loanSchedulerService;
        _ledgerRecordService = ledgerRecordService;
    }

    public async Task<IEnumerable<LoanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Loans.GetAllAsync(cancellationToken)).ToLoanResponses();
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchid, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Loans.GetActiveLoansAsync(branchid, cancellationToken);
    }

    public async Task<LoanResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(id, cancellationToken);
        if (loan == null)
            throw new NotFoundException($"Loan with id {id} not found.");
        return loan.ToLoanResponse();
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Loans.GetLoanByMemId(memberId, cancellationToken);
    }

    public async Task<Loan> AddLoanAsync(CreateLoanRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var member = await _unitOfWork.Members.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null)
            throw new NotFoundException($"Member with id {request.MemberId} not found.");

        var hasOpenLoan = await _unitOfWork.Loans.HasOpenLoanForMemberAsync(request.MemberId, cancellationToken);
        if (hasOpenLoan)
            throw new InvalidOperationException("This member already has an open loan.");

        var currentBalance = await _ledgerBalanceService.GetCurrentBalanceAsync(userId, cancellationToken);

        var requiredAmount = request.LoanAmount;

        if (currentBalance < requiredAmount)
            throw new InvalidOperationException("Insufficient funds to disburse the loan.");

        using var transaction = _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {

            var loan = new Loan
            (
                memberId: request.MemberId,
                loanAmount: request.LoanAmount,
                interestAmount: request.InterestAmount,
                processingFee: request.ProcessingFee,
                insuranceFee: request.InsuranceFee,
                isSavingEnabled: request.IsSavingEnabled,
                savingAmount: request.SavingAmount,
                totalAmount: request.TotalAmount,
                disbursementDate: request.DisbursementDate,
                collectionTerm: request.CollectionTerm,
                collectionStartDate: request.CollectionStartDate,
                noOfTerms: request.NoOfTerms,
                createdBy: userId
            );
            await _unitOfWork.Loans.AddLoanAsync(loan, cancellationToken);

            await _unitOfWork.CompleteAsync();

            // Generate Loan Schdulers

            if (loan.NoOfTerms > 0 && loan.CollectionStartDate.HasValue && !string.IsNullOrEmpty(loan.CollectionTerm))
            {
                await _loanSchedulerService.GenerateEMIScheduleAsync(loan.Id, userId, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Loan does not meet requirements for schedule generation. NoOfTerms: {loan.NoOfTerms}, CollectionStartDate: {loan.CollectionStartDate}, CollectionTerm: {loan.CollectionTerm}");
            }


            // Create Ledger Transaction for Loan Disbursement  

            await _ledgerRecordService.RecordWithdrawalAsync(
                paidFromUserId: userId,
                amount: loan.LoanAmount,
                paymentDate: loan.DisbursementDate?? DateTime.UtcNow,
                createdBy: userId,
                createdDate: DateTime.UtcNow,
                referenceId: loan.Id,
                comments: $"Loan disbursement for Loan ID: {loan.Id}, Member ID: {loan.MemberId}"
                );

            if (loan.ProcessingFee > 0)
            {
                await _ledgerRecordService.RecordDepositAsync(
                    paidToUserId: userId,
                    amount: loan.ProcessingFee,
                    paymentDate: loan.DisbursementDate?? DateTime.UtcNow,
                    createdBy: userId,
                    createdDate: DateTime.UtcNow,
                    referenceId: loan.Id,
                    transactionType: "Processing fee",
                    comments: $"Processing fee for Loan ID: {loan.Id}, from Member ID: {loan.MemberId}"
                );
            }

            if (loan.InsuranceFee > 0)
            {
                await _ledgerRecordService.RecordDepositAsync(
                    paidToUserId: userId,
                    amount: loan.InsuranceFee,
                    paymentDate: loan.DisbursementDate ?? DateTime.UtcNow,
                    createdBy: userId,
                    createdDate: DateTime.UtcNow,
                    referenceId: loan.Id,
                    transactionType: "Insurance fee",
                    comments: $"Insurance fee for Loan ID: {loan.Id}, from Member ID: {loan.MemberId}"
                );
            }

            await transaction.CommitAsync(cancellationToken);

            return loan;

        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Loans_MemberId_OpenLoanUnique", StringComparison.OrdinalIgnoreCase) == true)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("This member already has an open loan.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

    }

    public async Task<CloseLoanResponse> CloseLoanAsync(int loanId, int userId, CancellationToken cancellationToken = default)
    {
        var loan = await _unitOfWork.Loans.GetByIdAsync(loanId, cancellationToken);
        if (loan == null)
            throw new NotFoundException($"Loan with id {loanId} not found.");

        if (string.Equals(loan.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            return new CloseLoanResponse
            {
                LoanId = loan.Id,
                IsClosed = true,
                Status = loan.Status,
                ClosureDate = loan.ClosureDate,
            };
        }

        var hasOpenSchedulers = await _unitOfWork.Loans.HasOpenSchedulersAsync(loanId, cancellationToken);
        if (hasOpenSchedulers)
        {
            return new CloseLoanResponse
            {
                LoanId = loan.Id,
                IsClosed = false,
                Status = loan.Status,
                ClosureDate = loan.ClosureDate,
            };
        }

        loan.CloseLoan(userId);
        await _unitOfWork.CompleteAsync();

        return new CloseLoanResponse
        {
            LoanId = loan.Id,
            IsClosed = true,
            Status = "Closed",
            ClosureDate = loan.ClosureDate,
        };
    }
}