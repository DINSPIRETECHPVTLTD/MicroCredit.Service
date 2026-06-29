namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Hierarchical staff schedules report: Staff → POC → Member schedule lines.
/// </summary>
public class StaffSchedulesReportResponseDto
{
    public List<StaffSchedulesStaffNodeDto> Staff { get; set; } = new();
}

public class StaffSchedulesStaffNodeDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public List<StaffSchedulesPocNodeDto> Pocs { get; set; } = new();
}

public class StaffSchedulesPocNodeDto
{
    public int PocId { get; set; }
    public string PocFullName { get; set; } = string.Empty;
    public int CenterId { get; set; }
    public List<StaffReportMemberRowDto> Members { get; set; } = new();
}
