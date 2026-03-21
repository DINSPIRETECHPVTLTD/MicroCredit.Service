using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface ILedgerBalanceRepository
    {
        Task<IEnumerable<Ledger>> GetLedgerBalanceAsync(int orgId, CancellationToken cancellationToken = default);
        Task<Ledger?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task AddAsync(Ledger ledger, CancellationToken cancellationToken = default);

        Task<decimal> GetCurrentBalanceAsync(int userId, CancellationToken cancellationToken = default);
    }
}
