using MicroCredit.Domain.Model.User;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Mappings.ApplicationModel;

public static class CreateUserRequestExtensions
{
    /// <summary>Maps to entity; use passwordHash (e.g. BCrypt), not plain password.</summary>
    public static User ToUser(this CreateUserRequest request, int orgId, int? branchId, int createdBy, string passwordHash)
    {
        return new User(
            request.FirstName,
            request.SurName,
            request.Role,
            request.Email,
            passwordHash,
            orgId,
            request.Level,
            createdBy,
            string.Empty,
            request.PhoneNumber,
            request.Address1,
            request.Address2,
            request.City,
            request.State,
            request.PinCode,
            branchId);
    }
}
