using MicroCredit.Application.Model.Auth;

namespace MicroCredit.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Login(AuthRequest request);
}
