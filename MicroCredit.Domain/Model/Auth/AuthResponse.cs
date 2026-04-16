using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.Organization;

namespace MicroCredit.Domain.Model.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Mode { get; set; } = "ORG";
    public int? BranchId { get; set; }
    public required OrgResponse Organization { get; set; }
    public BranchResponse? Branch { get; set; }
}
