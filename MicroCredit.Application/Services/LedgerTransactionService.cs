using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Utilities;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Fund;

namespace MicroCredit.Application.Services
{
    public class LedgerTransactionService : ILedgerTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerRecordService _ledgerRecordService;
        private readonly IUserContext _userContext;

        public LedgerTransactionService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService, IUserContext userContext)
        {
            _unitOfWork = unitOfWork;
            _ledgerRecordService = ledgerRecordService;
            _userContext = userContext;
        }

        public async Task<IEnumerable<ExpenseResponse>> GetExpensesAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .LedgerTransaction
                .GetExpensesAsync(orgId, cancellationToken))
                .ToExpenseResponses();
        }

        public async Task CreateExpenseAsync(CreateExpenseRequest request, int createdByUserId, CancellationToken cancellationToken = default)
        {
            var paymentDate = ClientDateTimeConverter.NormalizeForStorage(request.PaymentDate, _userContext.TimeZoneId);
            var createdDate = ClientDateTimeConverter.NormalizeForStorage(request.CreatedDate, _userContext.TimeZoneId);
            
            await _ledgerRecordService.RecordExpenseAsync(
                request.PaidFromUserId,
                request.Amount,
                paymentDate,
                createdByUserId,
                createdDate,
                null,
                request.Comments,
                cancellationToken);
            // Keep Insurance_Claim_Financial_Summary.TotalExpenseAmount in sync with all expenses.
            await _unitOfWork.InsuranceClaimFinancialSummaries
                .RefreshTotalExpenseAmountAsync(cancellationToken);

            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<ExpenseResponse>> GetTransactionsByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .LedgerTransaction
                .GetByUserIdAsync(userId, cancellationToken))
                .ToExpenseResponses();
        }
    }
}
