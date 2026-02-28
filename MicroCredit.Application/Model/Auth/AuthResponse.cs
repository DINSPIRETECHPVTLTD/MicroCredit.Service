namespace MicroCredit.Application.Model.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
