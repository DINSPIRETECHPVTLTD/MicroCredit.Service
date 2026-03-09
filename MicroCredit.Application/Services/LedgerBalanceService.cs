using System;
using MicroCredit.Domain.Interfaces.Service;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Fund;
using MicroCredit.Application.Mappings.DomianEntity;

namespace MicroCredit.Application.Services
{
    public class LedgerBalanceService : ILedgerBalanceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LedgerBalanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<LedgerBalanceResponse>> GetLedgerBalancesAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .LedgerBalances
                .GetLedgerBalanceAsync(orgId, cancellationToken))
                .ToLedgerBalanceResponses();



        }
    }
}
