using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Model.Auth;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces;

namespace MicroCredit.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse?> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = _jwtTokenGenerator.GenerateToken(user);
        var userType = user.Level == UserLevel.Org ? "Organization" : "Branch";

        return new AuthResponse
        {
            Token = token,
            UserType = userType,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString()
        };
    }
}
