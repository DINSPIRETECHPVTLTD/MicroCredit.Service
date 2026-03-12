using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.Center;

namespace MicroCredit.Domain.Interfaces.Services
{
    public interface ICenterService
    {
        
        Task<IEnumerable<CenterResponse>> GetCentersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
        Task<CenterResponse> CreateCenterByBranchAsync(CreateCenterByBranch request, int branchId,int createdBy, CancellationToken cancellationToken = default);
    }
}
