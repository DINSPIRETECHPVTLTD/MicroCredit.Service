using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Model.Branch;
using MicroCredit.Domain.Interfaces;

namespace MicroCredit.Application.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<BranchResponse>> GetBranchsAsync(int orgId, CancellationToken cancellationToken = default)
    { 
        return (await _unitOfWork.Branches.GetBranchs(orgId, cancellationToken)).ToBranchResponses();
    }
}
