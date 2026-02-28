using MicroCredit.Application.Model.Organization;

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
    /// <summary>Current context: "ORG" or "BRANCH".</summary>
    public string Mode { get; set; } = "ORG";
    public required OrgResponse Organization { get; set; }
    /// <summary>Set when Mode is "BRANCH" (navigated to a branch); null when Mode is "ORG".</summary>
    public BranchResponse? Branch { get; set; }
}
