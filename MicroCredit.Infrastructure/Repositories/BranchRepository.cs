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

    public async Task<IReadOnlyList<string>> GetActiveDependencyNamesAsync(
        int branchId,
        int orgId,
        CancellationToken cancellationToken = default)
    {
        var activeDependencies = new List<string>();

        var hasActiveCenters = await (
            from c in _context.Centers
            join b in _context.Branches on c.BranchId equals b.Id
            where c.BranchId == branchId
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
            select c.Id
        ).AnyAsync(cancellationToken);
        if (hasActiveCenters)
            activeDependencies.Add("Centers");

        var hasActivePocs = await (
            from p in _context.POCs
            join c in _context.Centers on p.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where !p.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
            select p.Id
        ).AnyAsync(cancellationToken);
        if (hasActivePocs)
            activeDependencies.Add("POCs");

        var hasActiveMembers = await (
            from m in _context.Members
            join c in _context.Centers on m.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where !m.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
            select m.Id
        ).AnyAsync(cancellationToken);
        if (hasActiveMembers)
            activeDependencies.Add("Members");

        var hasActiveStaff = await _context.Users
            .AnyAsync(u =>
                u.BranchId == branchId &&
                !u.IsDeleted &&
                u.OrgId == orgId,
                cancellationToken);
        if (hasActiveStaff)
            activeDependencies.Add("Staff");

        var hasActiveLoans = await (
            from l in _context.Loans
            join m in _context.Members on l.MemberId equals m.Id
            join c in _context.Centers on m.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where !l.IsDeleted
                  && l.Status == "Active"
                  && !m.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
            select l.Id
        ).AnyAsync(cancellationToken);
        if (hasActiveLoans)
            activeDependencies.Add("Loans");

        return activeDependencies;
    }

    public Task CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _context.Branches.Add(branch);
        return Task.CompletedTask;
    }
    public Task UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _context.Branches.Update(branch);
        return Task.CompletedTask;
    }
}
