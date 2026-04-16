using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Member;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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

    public async Task<bool> ExistsByAadhaarAsync(string aadhaar, int? excludeMemberId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aadhaar))
            return false;

        var normalizedAadhaar = aadhaar.Trim();

        return await _context.Members
            .Where(m => m.Aadhaar != null && m.Aadhaar == normalizedAadhaar)
            .Where(m => !excludeMemberId.HasValue || m.Id != excludeMemberId.Value)
            .AnyAsync(cancellationToken);
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

    public async Task<IEnumerable<Member>> SearchMembersByBranchAsync(SearchMemberRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Members
        .Include(m => m.Center)
        .Include(m => m.POC)
        .Where(m => m.Center != null &&
                    m.Center.BranchId == request.BranchId &&
                    !m.IsDeleted);

        // Apply filters ONLY if any name is provided
        if (!string.IsNullOrWhiteSpace(request.FirstName) ||
            !string.IsNullOrWhiteSpace(request.MiddleName) ||
            !string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Where(m =>
                (!string.IsNullOrWhiteSpace(request.FirstName) && m.FirstName.Contains(request.FirstName)) ||
                (!string.IsNullOrWhiteSpace(request.MiddleName) && m.MiddleName.Contains(request.MiddleName)) ||
                (!string.IsNullOrWhiteSpace(request.LastName) && m.LastName.Contains(request.LastName))
            );
        }

        // Always sort by latest
        query = query.OrderByDescending(m => m.CreatedAt);

        // If NO filters ? take only 10
        if (string.IsNullOrWhiteSpace(request.FirstName) &&
            string.IsNullOrWhiteSpace(request.MiddleName) &&
            string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Take(10);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
