using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Center;

namespace MicroCredit.Application.Services;

public class CenterService : ICenterService
{
    private readonly IUnitOfWork _unitOfWork;

    public CenterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CenterResponse>> GetCentersByBranchAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Centers.GetCentersByBranchAsync(branchId, cancellationToken))
            .ToCenterResponses();
    }
}
