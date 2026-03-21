using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class LoanSchedulerService : ILoanSchedulerService
    {
        public readonly IUnitOfWork _unitOfWork;
        public LoanSchedulerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateEMIScheduleAsync(int loanId, int userId, CancellationToken cancellationToken = default)
        {

            var loan = await _unitOfWork.Loans.GetByIdAsync(loanId, cancellationToken)
                ?? throw new KeyNotFoundException($"Loan with Id {loanId} not found");
            
            var existingSchedules = await _unitOfWork.LoanSchedulers.GetByLoanId(loanId, cancellationToken);

            if (existingSchedules != null)
            {
                throw new InvalidOperationException("EMI schedule already exists for this loan");
            }

            if (loan.NoOfTerms <= 0)
            {
                throw new InvalidOperationException("Invalid number of terms");
            }

            if (loan.CollectionStartDate == null)
            {
                throw new InvalidOperationException("Collection start date is required");
            }

            if (string.IsNullOrEmpty(loan.CollectionTerm))
            {
                throw new InvalidOperationException("Collection term is required");
            }

            // Calculate payment amounts
            decimal totalLoanAmount = loan.LoanAmount + loan.InterestAmount;
            decimal principalPerInstallment = loan.LoanAmount / loan.NoOfTerms;
            decimal interestPerInstallment = loan.InterestAmount / loan.NoOfTerms;
            decimal paymentPerInstallment = totalLoanAmount / loan.NoOfTerms;

            var schedules = new List<LoanScheduler>();
            DateTime currentDate = loan.CollectionStartDate.Value;

            for (int i = 1; i <= loan.NoOfTerms; i++)
            {
                var schedule = new LoanScheduler
                (
                    loanId: loanId,
                    scheduleDate: currentDate,
                    paymentAmount: 0,
                    principalAmount: 0,
                    interestAmount: 0,
                    installmentNo: i,
                    createdBy: userId,
                    actualEmiAmount: Math.Round(paymentPerInstallment, 2),
                    actualPrincipalAmount: Math.Round(principalPerInstallment, 2),
                    actualInterestAmount: Math.Round(interestPerInstallment, 2),
                    savingAmount: loan.IsSavingEnabled ? Math.Round(loan.SavingAmount / loan.NoOfTerms, 2) : 0
                );

                schedules.Add(schedule);

                // Calculate next payment date based on collection term
                currentDate = CalculateNextPaymentDate(currentDate, loan.CollectionTerm);
            }

            // Adjust last installment to account for rounding differences
            if (schedules.Any())
            {
                var lastSchedule = schedules.Last();
                var totalScheduledPrincipal = schedules.Sum(s => s.PrincipalAmount);
                var totalScheduledInterest = schedules.Sum(s => s.InterestAmount);

                decimal principalAdjustment = loan.LoanAmount - totalScheduledPrincipal;
                decimal interestAdjustment = loan.InterestAmount - totalScheduledInterest;

                // Use an AdjustAmounts method or re-create the last schedule with corrected values
                lastSchedule.AdjustAmounts(
                    newPrincipal: lastSchedule.ActualPrincipalAmount + principalAdjustment,
                    newInterest: lastSchedule.ActualInterestAmount + interestAdjustment
                );
            }

            await _unitOfWork.LoanSchedulers.AddRangeAsync(schedules, cancellationToken);
            await _unitOfWork.CompleteAsync();

        }

        private DateTime CalculateNextPaymentDate(DateTime currentDate, string collectionTerm)
        {
            return collectionTerm.ToLower() switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "biweekly" or "bi-weekly" => currentDate.AddDays(14),
                "monthly" => currentDate.AddMonths(1),
                "quarterly" => currentDate.AddMonths(3),
                "half-yearly" or "semi-annual" => currentDate.AddMonths(6),
                "yearly" or "annual" => currentDate.AddYears(1),
                _ => currentDate.AddDays(7) // Default to weekly
            };
        }

    }
}
