using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Interfaces;

namespace MicroCredit.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Users.GetOrgUsersAsync(orgId, cancellationToken)).ToUserResponses();
    }
    public async Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default)
    {
        return (await _unitOfWork.Users.GetBranchUsersAsync(orgId, branchId, cancellationToken)).ToUserResponses();
    }

}
