using MicroCredit.Domain.Model.Branch;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IBranchsService
{
    Task<IEnumerable<BranchResponse>> GetBranchsAsync(int orgId, CancellationToken cancellationToken = default);
}
