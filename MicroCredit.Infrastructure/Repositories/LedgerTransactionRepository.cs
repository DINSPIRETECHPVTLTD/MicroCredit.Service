using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Infrastructure.Repositories
{
    public class LedgerTransactionRepository : ILedgerTransactionRepository
    {
        private readonly MicroCreditDbContext _context;

        public LedgerTransactionRepository(MicroCreditDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<LedgerTransaction>> GetExpensesAsync(int orgId, CancellationToken cancellationToken = default)
        {

            return await _context.LedgerTransactions
                .Where(lt => (lt.FromUser.OrgId == orgId || lt.ToUser.OrgId == orgId) && lt.TransactionType == "Expense")
                .ToListAsync(cancellationToken);
        }
    }
}
