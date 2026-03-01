using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(MicroCreditDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default)
    {
        var orgRoles = new[] { (int)UserRole.Owner, (int)UserRole.Investor };
        return await _context.Users
            .Where(u => u.OrgId == orgId && !u.IsDeleted && orgRoles.Contains((int)u.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default)
    {
        var branchRoles = new[] { (int)UserRole.BranchAdmin, (int)UserRole.Staff };
        return await _context.Users
            .Where(u => u.OrgId == orgId && !u.IsDeleted && u.BranchId == branchId && branchRoles.Contains((int)u.Role))
            .ToListAsync(cancellationToken);
    }
}
