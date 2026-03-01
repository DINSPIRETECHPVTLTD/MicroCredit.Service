using MicroCredit.Application.Model.User;

namespace MicroCredit.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> CreateUserAsync(CreateUserResponse response, int orgId, int? branchId, int createdBy, CancellationToken cancellationToken);
    Task<IEnumerable<UserResponse>> UpdateUserAsync(UpdateUserResponse response, int orgId, int? branchId, int modifiedBy, CancellationToken cancellationToken);
}
