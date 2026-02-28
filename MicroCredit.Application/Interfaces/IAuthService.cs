using MicroCredit.Application.Model.Auth;

namespace MicroCredit.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default);
}
