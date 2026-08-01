namespace MicroCredit.Domain.Entities;

/// <summary>
/// Central loan schedule totals after partial-payment splits (parent ActualEmi shrinks; remainder is a child row).
/// </summary>
public static class LoanSchedulerSummaryCalculator
{
    public static decimal TotalAmountPaid(IEnumerable<LoanScheduler> schedulers)
        => schedulers
            .Where(s => s.Status == LoanSchedulerStatus.Paid || s.Status == LoanSchedulerStatus.Partial)
            .Sum(s => s.PaymentAmount);

    public static decimal RemainingBalance(IEnumerable<LoanScheduler> schedulers)
        => schedulers
            .Where(s => s.Status == LoanSchedulerStatus.NotPaid)
            .Sum(s => s.ActualEmiAmount);

    public static decimal SchedulerTotalAmount(IEnumerable<LoanScheduler> schedulers)
        => schedulers.Sum(s => s.ActualEmiAmount);

    public static int CountBaseTerms(IEnumerable<LoanScheduler> schedulers)
        => schedulers.Count(s => s.SubInstallmentSequence == 0);

    public static int CountFullyPaidBaseTerms(IEnumerable<LoanScheduler> schedulers)
        => schedulers.Count(s => s.SubInstallmentSequence == 0 && s.Status == LoanSchedulerStatus.Paid);

    public static int CountPartialBaseTerms(IEnumerable<LoanScheduler> schedulers)
        => schedulers.Count(s => s.SubInstallmentSequence == 0 && s.Status == LoanSchedulerStatus.Partial);

    public static int CountOutstandingInstallmentWeeks(IEnumerable<LoanScheduler> schedulers)
        => schedulers
            .Where(s => s.Status == LoanSchedulerStatus.NotPaid)
            .Select(s => s.InstallmentNo)
            .Distinct()
            .Count();

    public static string FormatTermsProgress(IEnumerable<LoanScheduler> schedulers)
    {
        var total = CountBaseTerms(schedulers);
        var paid = CountFullyPaidBaseTerms(schedulers);
        var partial = CountPartialBaseTerms(schedulers);
        return $"{paid + partial}/{total}";
    }
}
