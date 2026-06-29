namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Member schedule line for the staff schedules report (7-day window, branch via member center).
/// </summary>
public class StaffReportMemberRowDto
{
    public int MemberId { get; set; }
    public string? MemberCode { get; set; }
    public int PocId { get; set; }
    public string MemberFullName { get; set; } = string.Empty;
    public int LoanId { get; set; }
    public string LoanStatus { get; set; } = string.Empty;
    public int LoanSchedulerId { get; set; }
    public DateTime ScheduleDate { get; set; }
    public decimal ActualEmiAmount { get; set; }
    public string LoanSchedulerStatus { get; set; } = string.Empty;
}
