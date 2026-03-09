using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface ILedgerTransactionRepository
    {
        Task<IEnumerable<LedgerTransaction>> GetExpensesAsync(int orgId, CancellationToken cancellationToken = default);
    }
}
