using MicroCredit.Application.Mappings.DomianEntity;
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

        public LedgerTransactionService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService)
        {
            _unitOfWork = unitOfWork;
            _ledgerRecordService = ledgerRecordService;
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
            
            await _ledgerRecordService.RecordExpenseAsync(
                request.PaidFromUserId,
                request.Amount,
                request.PaymentDate,
                createdByUserId,
                request.CreatedDate,
                null,
                request.Comments,
                cancellationToken);

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
