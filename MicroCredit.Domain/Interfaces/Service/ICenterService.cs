using MicroCredit.Domain.Model.Center;
using MicroCredit.Domain.Interfaces.Services;

namespace MicroCredit.Domain.Interfaces.Services
{
    public interface ICenterService
    {
        Task<IEnumerable<CenterResponse>> GetCentersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    }
}
