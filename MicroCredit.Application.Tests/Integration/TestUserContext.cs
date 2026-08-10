using MicroCredit.Domain.Common;

namespace MicroCredit.Application.Tests.Integration;

/// <summary>
/// Mutable <see cref="IUserContext"/> for DB-backed recovery posting integration tests.
/// </summary>
public sealed class TestUserContext : IUserContext
{
    public int UserId { get; set; }
    public int OrgId { get; set; }
    public int? BranchId { get; set; }
    public string TimeZoneId { get; set; } = "UTC";

    public (int OrgId, int BranchId) GetBranchContext()
    {
        if (!BranchId.HasValue)
            throw new InvalidOperationException("Branch context is required. Navigate to a branch first.");
        return (OrgId, BranchId.Value);
    }
}
