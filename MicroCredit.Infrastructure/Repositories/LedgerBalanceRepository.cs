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

        public async Task<Ledger?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Ledgers
                .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(Ledger ledger, CancellationToken cancellationToken = default)
        {
            await _context.Ledgers.AddAsync(ledger, cancellationToken);
        }

        public async Task CreateFundTransfer( CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<decimal> GetCurrentBalanceAsync(int userId, CancellationToken cancellationToken = default)
        {
            var ledger = await _context.Ledgers
                .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);
            return ledger?.Amount ?? 0m;
        }
    }
}
