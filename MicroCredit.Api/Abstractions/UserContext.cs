using MicroCredit.Domain.Common;
using System.Security.Claims;

namespace MicroCredit.Api.Abstractions;

/// <summary>
/// Reads user context from the current request's claims (JWT).
/// </summary>
public class UserContext : IUserContext
{
    private const string TimeZoneHeaderName = "X-Client-TimeZone";
    private readonly int _userId;
    private readonly int _orgId;
    private readonly int? _branchId;
    private readonly string _timeZoneId;

    public UserContext(IHttpContextAccessor accessor)
    {
        var request = accessor.HttpContext?.Request;
        var user = accessor.HttpContext?.User;
        _userId = ParseClaim(user, ClaimTypes.NameIdentifier);
        _orgId = ParseClaim(user, "OrgId");
        var branchValue = user?.FindFirstValue("BranchId");
        _branchId = string.IsNullOrEmpty(branchValue) || !int.TryParse(branchValue, out var bid) ? null : bid;
        _timeZoneId = NormalizeTimeZoneId(request?.Headers[TimeZoneHeaderName].FirstOrDefault());
    }

    public int UserId => _userId;
    public int OrgId => _orgId;
    public int? BranchId => _branchId;
    public string TimeZoneId => _timeZoneId;

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

    private static string NormalizeTimeZoneId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "UTC";

        var trimmed = value.Trim();
        if (IsValidTimeZone(trimmed))
            return trimmed;

        if (trimmed.Equals("Asia/Kolkata", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Asia/Calcutta", StringComparison.OrdinalIgnoreCase))
            return "India Standard Time";

        if (trimmed.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase))
            return "UTC";

        return "UTC";
    }

    private static bool IsValidTimeZone(string id)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
