using MicroCredit.Application.Model.User;

namespace MicroCredit.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default);
}
