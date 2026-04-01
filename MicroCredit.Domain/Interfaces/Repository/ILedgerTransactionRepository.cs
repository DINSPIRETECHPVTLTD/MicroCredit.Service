using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Fund;
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
        Task AddInvestmentTransactionAsync(LedgerTransaction ledgerTransaction, CancellationToken cancellationToken = default);
        Task AddExpenseAsync(LedgerTransaction ledgerTransaction, CancellationToken cancellationToken = default);

        Task<IEnumerable<LedgerTransaction>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task AddAsync(LedgerTransaction transaction, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTypeAndReferenceIdAsync(
            string transactionType,
            int referenceId,
            CancellationToken cancellationToken = default);
    }
}
