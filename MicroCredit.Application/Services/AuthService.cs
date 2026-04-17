using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Auth;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace MicroCredit.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _configuration = configuration;
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

        var requestedMode = (request.Mode ?? string.Empty).Trim().ToUpperInvariant();
        var configuredMode = (_configuration["Auth:DefaultMode"] ?? "ORG").Trim().ToUpperInvariant();
        var effectiveMode = requestedMode is "ORG" or "BRANCH" ? requestedMode : configuredMode;
        var isBranchMode = effectiveMode == "BRANCH";
        var isOwner = user.Role == UserRole.Owner;
        var isBranchRole = user.Role == UserRole.BranchAdmin || user.Role == UserRole.Staff;

        if (!isOwner && isBranchRole && !isBranchMode)
            throw new UnauthorizedAccessException("Branch users can login only in Branch mode.");

        var loginMode = "ORG";
        int? contextBranchId = null;

        if (isBranchMode && isBranchRole)
        {
            if (!user.BranchId.HasValue)
                throw new UnauthorizedAccessException("Branch user is missing branch assignment.");
            loginMode = "BRANCH";
            contextBranchId = user.BranchId.Value;
        }

        var token = _jwtTokenGenerator.GenerateToken(user, contextBranchId, loginMode);
        var contextBranch = contextBranchId.HasValue
            ? await _unitOfWork.Branches.GetByIdAndOrgIdAsync(contextBranchId.Value, user.OrgId, cancellationToken)
            : null;

        return user.ToAuthResponse(token, contextBranch);
    }

    public async Task<AuthResponse?> NavigateToBranchAsync(int userId, int branchId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;
        if (user.Role != UserRole.Owner && user.Role != UserRole.BranchAdmin && user.Role != UserRole.Staff)
            throw new UnauthorizedAccessException("Role is not allowed to switch branch mode.");

        if ((user.Role == UserRole.BranchAdmin || user.Role == UserRole.Staff) &&
            user.BranchId.HasValue &&
            user.BranchId.Value != branchId)
            throw new UnauthorizedAccessException("You can switch only to your assigned branch.");

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
        if (user.Role != UserRole.Owner)
            throw new UnauthorizedAccessException("Only Owner can switch to Org mode.");

        var token = _jwtTokenGenerator.GenerateToken(user, contextBranchId: null, "ORG");
        return user.ToAuthResponse(token, contextBranch: null);
    }
}
