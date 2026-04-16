using System.Security.Claims;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Api.Helpers;

/// <summary>
/// Static helper to read user context from JWT claims (UserId, OrgId, BranchId, Mode).
/// Use with the current <see cref="ClaimsPrincipal"/> (e.g. ControllerBase.User).
/// </summary>
public static class UserClaimsHelper
{
    private const string OrgIdClaim = "OrgId";
    private const string BranchIdClaim = "BranchId";
    private const string ModeClaim = "Mode";

    /// <summary>
    /// Gets the current user's Id from claims.
    /// </summary>
    /// <returns>UserId if present and valid; otherwise null.</returns>
    public static int? GetUserId(ClaimsPrincipal? user)
    {
        if (user == null) return null;
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// Gets the current user's Id and OrgId from claims.
    /// </summary>
    /// <returns>(UserId, OrgId) if both present and valid; otherwise null.</returns>
    public static (int UserId, int OrgId)? GetUserIdAndOrgId(ClaimsPrincipal? user)
    {
        var userId = GetUserId(user);
        if (userId == null) return null;
        var orgIdValue = user?.FindFirstValue(OrgIdClaim);
        if (string.IsNullOrEmpty(orgIdValue) || !int.TryParse(orgIdValue, out var orgId))
            return null;
        return (userId.Value, orgId);
    }

    /// <summary>
    /// Gets the current user's Id, OrgId and optional BranchId from claims.
    /// BranchId is null when mode is ORG or when BranchId claim is missing/empty.
    /// </summary>
    /// <returns>(UserId, OrgId, BranchId?) if UserId and OrgId are valid; BranchId may be null.</returns>
    public static (int UserId, int OrgId, int? BranchId)? GetUserIdOrgIdAndBranchId(ClaimsPrincipal? user)
    {
        var userIdAndOrgId = GetUserIdAndOrgId(user);
        if (userIdAndOrgId == null) return null;

        var (userId, orgId) = userIdAndOrgId.Value;
        var branchIdValue = user?.FindFirstValue(BranchIdClaim);
        int? branchId = null;
        if (!string.IsNullOrEmpty(branchIdValue) && int.TryParse(branchIdValue, out var bid))
            branchId = bid;

        return (userId, orgId, branchId);
    }

    public static UserRole? GetUserRole(ClaimsPrincipal? user)
    {
        var value = user?.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<UserRole>(value, true, out var role) ? role : null;
    }

    public static string GetMode(ClaimsPrincipal? user)
    {
        var mode = user?.FindFirstValue(ModeClaim);
        return string.IsNullOrWhiteSpace(mode) ? "ORG" : mode.Trim().ToUpperInvariant();
    }

    public static bool IsOwner(ClaimsPrincipal? user) => GetUserRole(user) == UserRole.Owner;

    public static bool IsBranchUser(ClaimsPrincipal? user)
    {
        var role = GetUserRole(user);
        return role == UserRole.BranchAdmin || role == UserRole.Staff;
    }
}
