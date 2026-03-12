using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface ICenterService
    {
        Task<IEnumerable<CenterResponse>> GetCentersAsync(int branchId, CancellationToken cancellationToken = default);
        Task<CenterResponse> CreateCenterAsync(CreateCenterRequest request, IUserContext userContext, CancellationToken cancellationToken = default);
        Task<CenterResponse> UpdateCenterAsync(int centerId, UpdateCenterRequest request, IUserContext context, CancellationToken cancellationToken = default);
        Task<bool> MarkAsInactive(int CenterId, int modifiedby, CancellationToken cancellationToken = default);
    }
}
