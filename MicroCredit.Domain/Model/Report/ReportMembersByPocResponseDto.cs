namespace MicroCredit.Domain.Model.Report;

public class ReportMembersByPocResponseDto
{
    public int MemberId { get; set; }
    public string MembersFullName { get; set; } = string.Empty;
    public decimal ActualEmiAmount { get; set; }
    /// <summary>Loan schedule due date (UTC/local per DB). Used with today/tomorrow window filter.</summary>
    public DateTime ScheduleDate { get; set; }
}
