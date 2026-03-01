using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.User;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IUsersService
{
    Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, IUserContext context, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(int id, UpdateUserRequest request, IUserContext context, CancellationToken cancellationToken = default);
}
