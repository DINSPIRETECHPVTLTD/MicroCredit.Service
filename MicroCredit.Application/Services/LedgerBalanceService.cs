using System;
using MicroCredit.Domain.Interfaces.Service;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Fund;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MicroCredit.Application.Services
{
    public class LedgerBalanceService : ILedgerBalanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerRecordService _ledgerRecordService;

        public LedgerBalanceService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService)
        {
            _unitOfWork = unitOfWork;
            _ledgerRecordService = ledgerRecordService;
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
            await _ledgerRecordService.RecordTransferAsync(request.PaidFromUserId, request.PaidToUserId, request.Amount,request.PaymentDate, createdByUserId, request.CreatedDate, null, request.Comments);

            await _unitOfWork.CompleteAsync();
        }

        public async Task<decimal> GetCurrentBalanceAsync(int userId, CancellationToken cancellationToken = default)
        {
            var balance = await _unitOfWork.LedgerBalances.GetCurrentBalanceAsync(userId, cancellationToken);
            return balance;
        }
    }
}
