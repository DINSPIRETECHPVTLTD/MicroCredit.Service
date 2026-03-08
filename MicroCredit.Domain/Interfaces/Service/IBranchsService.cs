using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.User;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IBranchsService
{
    Task<IEnumerable<BranchResponse>> GetBranchsAsync(int orgId, CancellationToken cancellationToken = default);
    Task<BranchResponse> CreateBranchAsync(CreateBranchRequest request,IUserContext userContext, CancellationToken cancellationToken = default);
}
