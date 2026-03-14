
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class POCRepository : IPOCRepository
{
    private readonly MicroCreditDbContext _context;

    public POCRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<POC?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.POCs
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<POC>> GetByBranchIdAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.POCs
      .Include(p => p.Center)                          // load Center
      .Where(p => p.Center != null &&
                  p.Center.BranchId == branchId && !p.IsDeleted)       // filter by branch
      .AsNoTracking()
      .ToListAsync(cancellationToken);
    }
    public Task CreateAsync(POC poc, CancellationToken cancellationToken = default)
    {
        _context.POCs.Add(poc);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(POC poc, CancellationToken cancellationToken = default)
    {
        _context.POCs.Update(poc);
        return Task.CompletedTask;
    }
}
