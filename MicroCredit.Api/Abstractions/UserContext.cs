using MicroCredit.Domain.Common;
using System.Security.Claims;

namespace MicroCredit.Api.Abstractions;

/// <summary>
/// Reads user context from the current request's claims (JWT).
/// </summary>
public class UserContext : IUserContext
{
    private readonly int _userId;
    private readonly int _orgId;
    private readonly int? _branchId;

    public UserContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        _userId = ParseClaim(user, ClaimTypes.NameIdentifier);
        _orgId = ParseClaim(user, "OrgId");
        var branchValue = user?.FindFirstValue("BranchId");
        _branchId = string.IsNullOrEmpty(branchValue) || !int.TryParse(branchValue, out var bid) ? null : bid;
    }

    public int UserId => _userId;
    public int OrgId => _orgId;
    public int? BranchId => _branchId;

    public (int OrgId, int BranchId) GetBranchContext()
    {
        if (!_branchId.HasValue)
            throw new InvalidOperationException("Branch context is required. Navigate to a branch first.");
        return (_orgId, _branchId.Value);
    }

    private static int ParseClaim(ClaimsPrincipal? user, string claimType)
    {
        var value = user?.FindFirstValue(claimType);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
