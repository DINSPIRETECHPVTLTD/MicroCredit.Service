using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAndOrgIdAsync(int branchId, int orgId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Branch>> GetBranchs(int orgId, CancellationToken cancellationToken = default);
    Task CreateAsync(Branch branch, CancellationToken cancellationToken = default);
}
