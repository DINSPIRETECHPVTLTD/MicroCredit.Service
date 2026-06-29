namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// POC row with collecting staff for the staff schedules report (branch via POC center).
/// </summary>
public class StaffReportPocRowDto
{
    public int PocId { get; set; }
    public int PocStaffId { get; set; }
    public int UserId { get; set; }
    public string PocFullName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public int CenterId { get; set; }
    public int BranchId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
