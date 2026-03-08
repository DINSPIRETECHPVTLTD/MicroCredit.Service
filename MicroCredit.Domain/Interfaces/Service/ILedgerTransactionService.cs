using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface ILedgerTransactionService
    {
        Task<IEnumerable<ExpenseResponse>> GetExpensesAsync(int orgId, CancellationToken cancellationToken = default);
    }
}
