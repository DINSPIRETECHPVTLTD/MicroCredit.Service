using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroCredit.Domain.Entities;

[Table("RecoveryPostingIdempotency")]
public class RecoveryPostingIdempotency
{
    [Key]
    public Guid ClientRequestId { get; private set; }

    [Required]
    public int OrgId { get; private set; }

    [Required]
    public int BranchId { get; private set; }

    [Required]
    public int UserId { get; private set; }

    [Required]
    [StringLength(64)]
    public string RequestHash { get; private set; } = string.Empty;

    [StringLength(4000)]
    public string? ResponseJson { get; private set; }

    [Required]
    public DateTime CreatedDate { get; private set; }

    private RecoveryPostingIdempotency() { }

    public RecoveryPostingIdempotency(
        Guid clientRequestId,
        int orgId,
        int branchId,
        int userId,
        string requestHash)
    {
        ClientRequestId = clientRequestId;
        OrgId = orgId;
        BranchId = branchId;
        UserId = userId;
        RequestHash = requestHash;
        CreatedDate = DateTime.UtcNow;
    }

    public void SetResponse(string responseJson)
    {
        ResponseJson = responseJson;
    }
}
