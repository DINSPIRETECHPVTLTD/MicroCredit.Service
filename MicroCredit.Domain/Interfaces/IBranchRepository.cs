using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAndOrgIdAsync(int branchId, int orgId, CancellationToken cancellationToken = default);
}
