using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.Fund;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Utilities;

namespace MicroCredit.Application.Services
{
    public class LedgerBalanceService : ILedgerBalanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerRecordService _ledgerRecordService;
        private readonly IUserContext _userContext;

        public LedgerBalanceService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService, IUserContext userContext)
        {
            _unitOfWork = unitOfWork;
            _ledgerRecordService = ledgerRecordService;
            _userContext = userContext;
        }

        public async Task<IEnumerable<LedgerBalanceResponse>> GetLedgerBalancesAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .LedgerBalances
                .GetLedgerBalanceAsync(orgId, cancellationToken))
                .ToLedgerBalanceResponses();
        }

        public async Task CreateFundTransferAsync(CreateFundTransferRequest request, int createdByUserId, CancellationToken cancellationToken = default)
        {
            var paymentDate = ClientDateTimeConverter.NormalizeForStorage(request.PaymentDate, _userContext.TimeZoneId);
            var createdDate = ClientDateTimeConverter.NormalizeForStorage(request.CreatedDate, _userContext.TimeZoneId);

            await _ledgerRecordService.RecordTransferAsync(request.PaidFromUserId, request.PaidToUserId, request.Amount, paymentDate, createdByUserId, createdDate, null, request.Comments);

            await _unitOfWork.CompleteAsync();
        }

        public async Task<decimal> GetCurrentBalanceAsync(int userId, CancellationToken cancellationToken = default)
        {
            var balance = await _unitOfWork.LedgerBalances.GetCurrentBalanceAsync(userId, cancellationToken);
            return balance;
        }
    }
}
