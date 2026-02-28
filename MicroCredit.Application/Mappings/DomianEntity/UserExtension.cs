using MicroCredit.Application.Model.Auth;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class UserExtension
{
    /// <summary>Build auth response for login (org context, no branch).</summary>
    public static AuthResponse ToAuthResponse(this User user, string token)
        => user.ToAuthResponse(token, contextBranch: null);

    /// <summary>Build auth response with optional branch context (for Navigate to Branch / Navigate to Org).</summary>
    public static AuthResponse ToAuthResponse(this User user, string token, Branch? contextBranch)
    {
        var userType = user.Level == UserLevel.Org ? "Organization" : "Branch";
        var mode = contextBranch != null ? "BRANCH" : "ORG";
        return new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            UserType = userType,
            Token = token,
            Mode = mode,
            Organization = user.Organization.ToOrgResponse(),
            Branch = contextBranch?.ToBranchResponse()
        };
    }
}
