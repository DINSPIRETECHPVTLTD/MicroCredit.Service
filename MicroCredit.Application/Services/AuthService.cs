using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Model.Auth;
using MicroCredit.Domain.Interfaces;

namespace MicroCredit.Application.Services;

public class AuthService: IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<AuthResponse> Login(AuthRequest request)
    {
        throw new NotImplementedException();
    }
}
