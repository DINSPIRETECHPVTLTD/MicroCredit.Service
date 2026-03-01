using MicroCredit.Application.Model.Branch;
namespace MicroCredit.Application.Interfaces;

public interface IBranchService
{
    Task<IEnumerable<BranchResponse>> GetBranchsAsync(int orgId, CancellationToken cancellationToken = default);
}
