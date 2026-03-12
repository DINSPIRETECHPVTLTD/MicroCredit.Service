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
    public class CenterRepository:ICenterRepository
    {
        private readonly MicroCreditDbContext _context;

        public CenterRepository(MicroCreditDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Center>> GetCenters(int branchId, CancellationToken cancellationToken = default)
        {
            return await _context.Centers
                .Where(b => b.BranchId == branchId && !b.IsDeleted)
                .ToListAsync(cancellationToken);
        }
        public Task<Center> GetByCenterId(int centerId, CancellationToken cancellationToken = default)
        {
            return _context.Centers
                .Where(b => b.Id == centerId && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }
        public Task CreateAsync(Center center, CancellationToken cancellationToken = default)
        {
            _context.Centers.Add(center);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Center center, CancellationToken cancellationToken = default)
        {
            _context.Centers.Update(center);
            return Task.CompletedTask;
        }
    }
}
