using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.LoanScheduler;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class LoanSchedulerExtention
{
    public static LoanSchedulerResponce ToLoanSchedulerResponce(this LoanScheduler loanScheduler)
    {
        return new LoanSchedulerResponce
        {
            LoanschedulerId = loanScheduler.LoanSchedulerId,
            LoanID = loanScheduler.LoanId,
            ScheduleDate = loanScheduler.ScheduleDate,
            PaymentDate = loanScheduler.PaymentDate,
            ActualEmiAmount = loanScheduler.ActualEmiAmount,
            ActualPrincipalAmount = loanScheduler.ActualPrincipalAmount,
            ActualInterestAmount = loanScheduler.ActualInterestAmount,
            PaymentAmount = loanScheduler.PaymentAmount,
            PrincipalAmount = loanScheduler.PrincipalAmount,
            InterestAmount = loanScheduler.InterestAmount,
            InstallmentNo = loanScheduler.InstallmentNo,
            SubInstallmentSequence = loanScheduler.SubInstallmentSequence,
            ParentLoanSchedulerId = loanScheduler.ParentLoanSchedulerId,
            InstallmentLabel = loanScheduler.GetInstallmentLabel(),
            Status = loanScheduler.Status.ToDbValue(),
            PaymentMode = loanScheduler.PaymentMode,
            Comments = loanScheduler.Comments,
        };
    }

    public static IEnumerable<LoanSchedulerResponce> ToLoanSchedulerResponces(this IEnumerable<LoanScheduler> loanSchedulers)
        => loanSchedulers.Select(ls => ls.ToLoanSchedulerResponce());
}
