using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Model.User;

public class UpdateUserRequest
{
    public string FirstName { get; private set; } = string.Empty;

    public string SurName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Address1 { get; private set; }
    public string? Address2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PinCode { get; private set; }
    public UserLevel Level { get; private set; }
}
