using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly MicroCreditDbContext _context;

    public BranchRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<Branch?> GetByIdAndOrgIdAsync(int branchId, int orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.OrgId == orgId && !b.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Branch>> GetBranchs(int orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .Where(b => b.OrgId == orgId && !b.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
