using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;

namespace MicroCredit.Application.Services;

public class BranchsService : IBranchsService
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<BranchResponse>> GetBranchsAsync(int orgId, CancellationToken cancellationToken = default)
    { 
        return (await _unitOfWork.Branches.GetBranchs(orgId, cancellationToken)).ToBranchResponses();
    }
}
