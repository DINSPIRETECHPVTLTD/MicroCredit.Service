using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class CenterRepository : ICenterRepository
{
    private readonly MicroCreditDbContext _context;

    public CenterRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Center>> GetCentersByBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Centers
            .Where(c => c.BranchId == branchId && !c.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Center center, CancellationToken cancellationToken = default)
    {
        await _context.Centers.AddAsync(center, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
