using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
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
        return await _context.Users
            .Where(u => u.OrgId == orgId && !u.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetOrgInvestorsAsync(int orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.OrgId == orgId && !u.IsDeleted
                && (u.Role == UserRole.Owner || u.Role == UserRole.Investor))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.OrgId == orgId && !u.IsDeleted && u.BranchId == branchId
                && (u.Role == UserRole.BranchAdmin || u.Role == UserRole.Staff))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetCollectedByUsersAsync(int orgId, int? branchId, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.OrgId == orgId && !u.IsDeleted);

        if (branchId.HasValue)
        {
            // In branch mode: keep Owner from org and branch staff/admin only for current branch.
            query = query.Where(u =>
                u.Role == UserRole.Owner ||
                ((u.Role == UserRole.BranchAdmin || u.Role == UserRole.Staff) && u.BranchId == branchId.Value));
        }
        else
        {
            query = query.Where(u =>
                u.Role == UserRole.Owner || u.Role == UserRole.BranchAdmin || u.Role == UserRole.Staff);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

     
}
