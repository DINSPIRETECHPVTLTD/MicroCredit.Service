namespace MicroCredit.Domain.Entities;

public enum LoanSchedulerStatus
{
    NotPaid = 0,
    Paid = 1,
    Partial = 2,
    Claimed = 3,
    Overdue = 4,
}
