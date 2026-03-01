using MicroCredit.Domain.Model.Auth;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse?> NavigateToBranchAsync(int userId, int branchId, CancellationToken cancellationToken = default);

    Task<AuthResponse?> NavigateToOrgAsync(int userId, CancellationToken cancellationToken = default);
}
