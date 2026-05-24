namespace MicroCredit.Domain.Model.Report;

/// <summary>
/// Staff collection schedule line: POC, collecting staff, member, and EMI row due today/tomorrow.
/// </summary>
public class StaffScheduleReportRowDto
{
    public int PocId { get; set; }
    public int PocStaffId { get; set; }
    public int UserId { get; set; }
    public string PocFullName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string MemberFullName { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public int CenterId { get; set; }
    public bool PocIsDeleted { get; set; }
    public int LoanSchedulerId { get; set; }
    public decimal ActualEmiAmount { get; set; }
    public DateTime ScheduleDate { get; set; }
    public int BranchId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
