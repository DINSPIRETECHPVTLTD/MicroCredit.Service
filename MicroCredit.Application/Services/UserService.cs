using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Mappings.ApplicationModel;
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

    public async Task<IEnumerable<UserResponse>> CreateUserAsync(CreateUserResponse response, int orgId, int? branchId, int createdBy, CancellationToken cancellationToken)
    {
        await _unitOfWork.Users.CreateAsync(response.ToUser(orgId, branchId, createdBy), cancellationToken);
        if (branchId != null)
        {
            return await GetBranchUsersAsync(orgId, branchId.Value, cancellationToken);
        }

        return await GetOrgUsersAsync(orgId, cancellationToken);
    }

    public async Task<IEnumerable<UserResponse>> UpdateUserAsync(UpdateUserResponse response, int orgId, int? branchId, int modifiedBy, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(response.Id);

        await _unitOfWork.Users.UpdateAsync(response.ToUser(user, orgId, branchId, modifiedBy), cancellationToken);
        if (branchId != null)
        {
            return await GetBranchUsersAsync(orgId, branchId.Value, cancellationToken);
        }

        return await GetOrgUsersAsync(orgId, cancellationToken);
    }

}
