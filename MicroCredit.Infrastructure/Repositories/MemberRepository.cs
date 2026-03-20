using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly MicroCreditDbContext _context;

    public MemberRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Members
            .Include(m => m.Center)
            .Include(m => m.POC)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Member>> GetMembersByBranchAsync(int branchId, CancellationToken cancellationToken)
    {
        return await _context.Members
            .Include(m => m.Center)
            .Include(m => m.POC)
            .Where(m => m.Center != null && m.Center.BranchId == branchId && !m.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task CreateAsync(Member member, CancellationToken cancellationToken = default)
    {
        _context.Members.Add(member);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        _context.Members.Update(member);
        return Task.CompletedTask;
    }
}
