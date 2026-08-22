namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Hierarchical staff schedules report: Staff → POC → Member schedule lines.
/// </summary>
public class StaffSchedulesReportResponseDto
{
    public StaffSchedulesTotalsDto Totals { get; set; } = new();
    public List<StaffSchedulesStaffNodeDto> Staff { get; set; } = new();
}

public class StaffSchedulesTotalsDto
{
    /// <summary>EMI due on the selected schedule date, excluding prepaid installments.</summary>
    public decimal TotalScheduleAmount { get; set; }
    /// <summary>Amount still unpaid (Not Paid full EMI, Partial remaining).</summary>
    public decimal TotalPendingAmount { get; set; }
    /// <summary>EMI marked Overdue on the selected schedule date.</summary>
    public decimal TotalOverdueAmount { get; set; }
    /// <summary>Collected on the same calendar day as ScheduleDate.</summary>
    public decimal TotalCollectedAmount { get; set; }
    /// <summary>Collected before ScheduleDate.</summary>
    public decimal TotalPreCollectedAmount { get; set; }
    /// <summary>Collected after ScheduleDate.</summary>
    public decimal TotalPostCollectedAmount { get; set; }
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
    public string CenterName { get; set; } = string.Empty;
    public List<StaffReportMemberRowDto> Members { get; set; } = new();
}
