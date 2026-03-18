using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Model.User;

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string SurName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public UserLevel Level { get; set; }
}
