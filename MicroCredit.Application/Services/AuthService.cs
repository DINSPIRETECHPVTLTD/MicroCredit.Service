using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Auth;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;

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

        if (user.Role == UserRole.Investor)
            throw new UnauthorizedAccessException("Unauthorized");

        return user.ToAuthResponse(_jwtTokenGenerator.GenerateToken(user));
    }

    public async Task<AuthResponse?> NavigateToBranchAsync(int userId, int branchId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        var branch = await _unitOfWork.Branches.GetByIdAndOrgIdAsync(branchId, user.OrgId, cancellationToken);
        if (branch == null)
            return null;

        var token = _jwtTokenGenerator.GenerateToken(user, branchId, "BRANCH");
        return user.ToAuthResponse(token, branch);
    }

    public async Task<AuthResponse?> NavigateToOrgAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        var token = _jwtTokenGenerator.GenerateToken(user, contextBranchId: null, "ORG");
        return user.ToAuthResponse(token, contextBranch: null);
    }
}
