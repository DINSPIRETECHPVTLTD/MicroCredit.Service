using MicroCredit.Application.Model.Auth;
using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class UserExtension
{
    public static AuthResponse ToAuthResponse(this User user)
    {
        var userType = user.Level == UserLevel.Org ? "Organization" : "Branch";
        return new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
        };
    }
    public static UserResponse ToUserResponse(this User user)
    { 
    return new UserResponse
    {
        UserId = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role.ToString(),
        PhoneNumber = user.PhoneNumber,
        Address1 = user.Address1,
        Address2 = user.Address2,
        City = user.City,
        State = user.State,
        ZipCode = user.ZipCode,
        OrgId = user.OrgId.ToString(),
        Level = user.Level.ToString(),
        BranchId = user.BranchId.HasValue ? user.BranchId.Value.ToString() : null,
        CreatedBy = user.CreatedBy.ToString(),
        CreatedAt = user.CreatedAt.ToString("o"), // ISO 8601 format
        ModifiedBy = user.ModifiedBy.HasValue ? user.ModifiedBy.Value.ToString() : null,
        ModifiedAt = user.ModifiedAt.HasValue ? user.ModifiedAt.Value.ToString("o") : null,
        IsDeleted = user.IsDeleted


    };

    }
    public static IEnumerable<UserResponse> ToUserResponses(
    this IEnumerable<User> users)
    {
        return users.Select(user => user.ToUserResponse());
        
    }
  
}
