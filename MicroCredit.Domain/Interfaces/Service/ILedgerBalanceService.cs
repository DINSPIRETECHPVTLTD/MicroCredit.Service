using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface ILedgerBalanceService
    {
        Task<IEnumerable<LedgerBalanceResponse>> GetLedgerBalancesAsync(int orgId, CancellationToken cancellationToken = default);
        Task CreateFundTransferAsync(CreateFundTransferRequest request, int createdByUserId, CancellationToken cancellationToken = default);
    }
}
