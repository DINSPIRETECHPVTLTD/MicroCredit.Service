namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Distinct staff user assigned as POC collector (CollectionBy) for a branch.
/// </summary>
public class PocCollectionStaffReportDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
}
