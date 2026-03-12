using MicroCredit.Application.Common;
using MicroCredit.Application.Mappings.ApplicationModel;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class CenterService: ICenterService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CenterService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CenterResponse>> GetCentersAsync(int branchId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork.Centers.GetCenters(branchId, cancellationToken)).ToCenterResponses();
        }
        public async Task<CenterResponse> CreateCenterAsync(CreateCenterRequest request, IUserContext userContext, CancellationToken cancellationToken = default)
        {
            if (!userContext.BranchId.HasValue)
                throw new InvalidOperationException("BranchId is required to create a center.");

            var entity = request.ToCenter(userContext.BranchId.Value, userContext.UserId);
            await _unitOfWork.Centers.CreateAsync(entity, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return entity.ToCenterResponse();
        }

        public async Task<CenterResponse> UpdateCenterAsync(int centerId, UpdateCenterRequest request, IUserContext context, CancellationToken cancellationToken = default)
        {
            var center = await _unitOfWork.Centers.GetByCenterId(centerId, cancellationToken);
            if (center == null)
                throw new NotFoundException("Center not found.");

            request.ToCenter(center, context.UserId);
            await _unitOfWork.Centers.UpdateAsync(center, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return center.ToCenterResponse();
        }

        public async Task<bool> MarkAsInactive(int CenterId, int modifiedby, CancellationToken cancellationToken = default)
        {
            var center = await _unitOfWork.Centers.GetByCenterId(CenterId, cancellationToken);
            if (center == null)
                throw new NotFoundException("Center not found.");
            center.MarkDeleted(modifiedby);

            await _unitOfWork.Centers.UpdateAsync(center, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return true;
        }

    }
}
