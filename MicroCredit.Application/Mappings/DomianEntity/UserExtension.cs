using MicroCredit.Application.Model.Auth;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class UserExtension
{
    public static AuthResponse ToAuthResponse(this User user, string token)
        => user.ToAuthResponse(token, contextBranch: null);

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
