using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.ApplicationModel;

public static class CreateUserResponseExtensions
{
    public static User ToUser(this CreateUserResponse createUserResponse, int orgId, int? branchId,int createdBy)
    {
        return new User(createUserResponse.FirstName,
            createUserResponse.SurName,
            createUserResponse.Role,
            createUserResponse.Email,
            createUserResponse.Password,
            orgId,
            createUserResponse.Level,
            createdBy,
            string.Empty,
            createUserResponse.PhoneNumber,
            createUserResponse.Address1,
            createUserResponse.Address2,
            createUserResponse.City,
            createUserResponse.State,
            createUserResponse.PinCode,
            branchId
            );
    
    }
}
