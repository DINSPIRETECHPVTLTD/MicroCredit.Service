namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Distinct staff users who collect for at least one non-deleted POC in the branch.
/// </summary>
public class PocCollectionStaffReportDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
}
