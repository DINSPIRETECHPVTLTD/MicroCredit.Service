using MicroCredit.Domain.Entities;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MicroCredit.Domain.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Infrastructure.Repositories
{
    public class LedgerBalanceRepository : ILedgerBalanceRepository
    {
        private readonly MicroCreditDbContext _context;

        public LedgerBalanceRepository(MicroCreditDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<Ledger>> GetLedgerBalanceAsync(int orgId, CancellationToken cancellationToken = default)
        {

            return await _context.Ledgers
                .Where(l => l.User.OrgId == orgId)
                .ToListAsync(cancellationToken);
        }
    }
}
