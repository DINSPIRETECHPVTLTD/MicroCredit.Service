namespace MicroCredit.Domain.Model.LoanScheduler;

public class LoanSchedulerResponce
{
    public int LoanschedulerId { get; set; }
    public int LoanID { get; set; }
    public DateTime ScheduleDate { get; set; }
    public DateTime? PaymentDate { get;  set; }
    public decimal ActualEmiAmount { get;  set; }
   
    public decimal PaymentAmount { get; set; }
   
    public int InstallmentNo { get; set; }
    public string Status { get; set; } = "Not Paid"; // Paid, Partial, Not Paid
    public string? PaymentMode { get; set; }
   
    public string? Comments { get;  set; }
   
    

}
   
