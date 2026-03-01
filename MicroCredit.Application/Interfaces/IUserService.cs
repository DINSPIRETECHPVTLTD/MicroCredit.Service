using MicroCredit.Application.Common;
using MicroCredit.Application.Model.User;

namespace MicroCredit.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetOrgUsersAsync(int orgId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetBranchUsersAsync(int orgId, int branchId, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserResponse request, IUserContext context, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(int id, UpdateUserResponse request, IUserContext context, CancellationToken cancellationToken = default);
}
