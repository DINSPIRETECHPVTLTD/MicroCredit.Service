using MicroCredit.Application.Model.Auth;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class UserExtension
{
    public static AuthResponse ToAuthResponse(this User user, string token)
    {
        var userType = user.Level == UserLevel.Org ? "Organization" : "Branch";
        return new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            UserType = userType,
            Token = token,
            Organization = user.Organization.ToOrgResponse()
        };
    }
}
