using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces;

public interface IJwtTokenGenerator
{
    /// <summary>Generates token for login (org context: BranchId from user, mode ORG).</summary>
    string GenerateToken(User user);

    /// <summary>Generates token with explicit context: branchId and mode (ORG or BRANCH).</summary>
    string GenerateToken(User user, int? contextBranchId, string mode);
}
