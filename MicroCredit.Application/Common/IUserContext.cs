namespace MicroCredit.Application.Common;

/// <summary>
/// Current request's user context (from JWT). Implemented in the API layer.
/// </summary>
public interface IUserContext
{
    int UserId { get; }
    int OrgId { get; }
    int? BranchId { get; }

    /// <summary>Gets (OrgId, BranchId). Throws if not in branch context (BranchId is null).</summary>
    (int OrgId, int BranchId) GetBranchContext();
}
