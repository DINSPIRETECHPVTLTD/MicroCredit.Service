using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.ApplicationModel;

public static class UpdateUserResponseExtension
{
    public static User ToUser(this UpdateUserResponse response, User existingUser, int orgId, int? branchId, int modifiedBy)
    {
        existingUser.UpdateDetails(response.FirstName,
            string.Empty,
            response.SurName,
            response.PhoneNumber,
            response.Address1,
            response.Address2,
            response.City,
            response.State,
            response.PinCode,
            branchId,
            modifiedBy);
        return existingUser;
    }
}
