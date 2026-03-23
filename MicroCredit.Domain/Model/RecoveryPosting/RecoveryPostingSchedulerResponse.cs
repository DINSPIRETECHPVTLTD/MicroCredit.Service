namespace MicroCredit.Domain.Model.RecoveryPosting;

/// <summary>
/// Loan scheduler row for recovery posting (loan, scheduler, center, branch).
/// </summary>
public class RecoveryPostingSchedulerResponse
{
    public int LoanId { get; set; }
    public int MemberId { get; set; }
    public string LoanStatus { get; set; } = string.Empty;

    public int LoanSchedulerId { get; set; }
    public int SchedulerLoanId { get; set; }

    public int InstallmentNo { get; set; }
    public DateTime ScheduleDate { get; set; }
    public DateTime? PaymentDate { get; set; }

    public decimal ActualEmiAmount { get; set; }
    public decimal ActualPrincipalAmount { get; set; }
    public decimal ActualInterestAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PrincipalAmount { get; set; }

    public string SchedulerStatus { get; set; } = string.Empty;
    public string? PaymentMode { get; set; }
    public int? CollectedBy { get; set; }
    public string? Comments { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public int CenterId { get; set; }
    public string CenterName { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}
