using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Application.Tests;

public class RecoveryPaymentSplitTests
{
    [Theory]
    [InlineData(416.665, 416.67)]
    [InlineData(208.325, 208.33)]
    [InlineData(41.675, 41.68)]
    public void Round2_Matches_Js_MathRound_HalfAwayFromZero_Positive(decimal input, decimal expected)
    {
        Assert.Equal(expected, RecoveryPaymentSplit.Round2(input));
    }

    [Fact]
    public void Recursive_Partial_833_33_166_67_pays_500_250_250_Reconciles()
    {
        decimal dueP = 833.33m;
        decimal dueI = 166.67m;
        decimal dueEmi = 1000m;

        var p1 = SplitAndShrink(ref dueP, ref dueI, ref dueEmi, 500m);
        Assert.Equal(416.67m, p1.PaidP);
        Assert.Equal(83.33m, p1.PaidI);
        Assert.Equal(416.66m, dueP);
        Assert.Equal(83.34m, dueI);
        Assert.Equal(500m, dueEmi);

        var p2 = SplitAndShrink(ref dueP, ref dueI, ref dueEmi, 250m);
        Assert.Equal(208.33m, p2.PaidP);
        Assert.Equal(41.67m, p2.PaidI);
        Assert.Equal(208.33m, dueP);
        Assert.Equal(41.67m, dueI);
        Assert.Equal(250m, dueEmi);

        var p3 = SplitAndShrink(ref dueP, ref dueI, ref dueEmi, 250m);
        Assert.Equal(208.33m, p3.PaidP);
        Assert.Equal(41.67m, p3.PaidI);
        Assert.Equal(0m, dueP);
        Assert.Equal(0m, dueI);
        Assert.Equal(0m, dueEmi);

        Assert.Equal(833.33m, p1.PaidP + p2.PaidP + p3.PaidP);
        Assert.Equal(166.67m, p1.PaidI + p2.PaidI + p3.PaidI);
        Assert.Equal(1000m, p1.PaidP + p1.PaidI + p2.PaidP + p2.PaidI + p3.PaidP + p3.PaidI);
    }

    [Fact]
    public void Split_333_33_Style_Pay_100()
    {
        var (p, i) = RecoveryPaymentSplit.CalculatePaymentSplitFromSchedule(
            333.33m, 266.66m, 66.67m, 100m);
        Assert.Equal(80m, p);
        Assert.Equal(20m, i);
    }

    [Fact]
    public void Split_ZeroPayment_ReturnsZeros()
    {
        var (p, i) = RecoveryPaymentSplit.CalculatePaymentSplitFromSchedule(
            1000m, 800m, 200m, 0m);
        Assert.Equal(0m, p);
        Assert.Equal(0m, i);
    }

    [Fact]
    public void FormatInstallmentLabel_BaseAndChildren()
    {
        Assert.Equal("10", LoanSchedulerCollectionRules.FormatInstallmentLabel(10, 0));
        Assert.Equal("10_1", LoanSchedulerCollectionRules.FormatInstallmentLabel(10, 1));
        Assert.Equal("10_2", LoanSchedulerCollectionRules.FormatInstallmentLabel(10, 2));
    }

    [Fact]
    public void IsBase_And_IsPaymentHistory_Predicates()
    {
        Assert.True(LoanSchedulerCollectionRules.IsBase(null, 0));
        Assert.False(LoanSchedulerCollectionRules.IsBase(1, 1));
        Assert.True(LoanSchedulerCollectionRules.IsPaymentHistory(10, LoanSchedulerStatus.Partial, 500m));
        Assert.False(LoanSchedulerCollectionRules.IsPaymentHistory(null, LoanSchedulerStatus.Partial, 500m));
    }

    [Fact]
    public void Carried_Overdue_With_PaymentDate_And_LaterBase_Does_Not_Block()
    {
        Assert.False(
            LoanSchedulerCollectionRules.BlocksSequentialCollection(
                LoanSchedulerStatus.Overdue,
                actualEmiAmount: 1000m,
                paymentDate: DateTime.UtcNow,
                hasLaterBaseInstallment: true));
        Assert.False(
            LoanSchedulerCollectionRules.IsUntransferredOverdue(
                LoanSchedulerStatus.Overdue,
                paymentDate: DateTime.UtcNow,
                hasLaterBaseInstallment: true));
    }

    [Fact]
    public void Untransferred_Overdue_Still_Blocks()
    {
        Assert.True(
            LoanSchedulerCollectionRules.BlocksSequentialCollection(
                LoanSchedulerStatus.Overdue,
                actualEmiAmount: 1000m,
                paymentDate: null,
                hasLaterBaseInstallment: true));
        Assert.True(
            LoanSchedulerCollectionRules.BlocksSequentialCollection(
                LoanSchedulerStatus.Overdue,
                actualEmiAmount: 1000m,
                paymentDate: DateTime.UtcNow,
                hasLaterBaseInstallment: false));
    }

    [Fact]
    public void Earlier_NotPaid_With_Actual_Still_Blocks()
    {
        Assert.True(
            LoanSchedulerCollectionRules.BlocksSequentialCollection(
                LoanSchedulerStatus.NotPaid,
                actualEmiAmount: 1000m,
                paymentDate: null,
                hasLaterBaseInstallment: true));
        Assert.False(
            LoanSchedulerCollectionRules.BlocksSequentialCollection(
                LoanSchedulerStatus.NotPaid,
                actualEmiAmount: 0m,
                paymentDate: null,
                hasLaterBaseInstallment: true));
    }

    private static (decimal PaidP, decimal PaidI) SplitAndShrink(
        ref decimal dueP, ref decimal dueI, ref decimal dueEmi, decimal payment)
    {
        var (paidP, paidI) = RecoveryPaymentSplit.CalculatePaymentSplitFromSchedule(
            dueEmi, dueP, dueI, payment);
        dueP = RecoveryPaymentSplit.Round2(dueP - paidP);
        dueI = RecoveryPaymentSplit.Round2(dueI - paidI);
        dueEmi = RecoveryPaymentSplit.Round2(dueP + dueI);
        return (paidP, paidI);
    }
}
