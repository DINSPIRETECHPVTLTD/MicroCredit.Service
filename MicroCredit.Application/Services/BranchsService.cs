using MicroCredit.Application.Mappings.ApplicationModel;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.User;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<BranchResponse> CreateBranchAsync(CreateBranchRequest request,IUserContext userContext,CancellationToken cancellationToken = default)
    {        var entity = request.ToBranch(userContext.OrgId,userContext.UserId);
         await _unitOfWork.Branches.CreateAsync(entity, cancellationToken);
        return entity.ToBranchResponse();
    }
}
