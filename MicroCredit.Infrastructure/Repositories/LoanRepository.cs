using System.Collections.Generic;
using System.Linq;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Loan;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly MicroCreditDbContext _context;

    public LoanRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

    }

    public async Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Where(l => !l.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default)
    {
        // Load entities from Loans (Status column is on Loans) and map in memory — never use SqlQueryRaw<ActiveLoanResponse>
        // against sp_MemberLoanReport: proc columns must match the DTO exactly or EF throws "FromSql" / missing Status.
        var loans = await _context.Loans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(l => l.Member)
            .Include(l => l.LoanSchedulers)
            .Where(loan => !loan.IsDeleted && loan.MemberId == memberId)
            .OrderBy(loan => loan.Id)
            .ToListAsync(cancellationToken);

        return loans
            .Where(loan =>
            {
                var s = loan.Status.Trim().ToUpperInvariant();
                return s is "ACTIVE" or "PENDING" or "CLAIMED";
            })
            .Select(MapLoanToActiveLoanResponse)
            .ToList();
    }

    private static ActiveLoanResponse MapLoanToActiveLoanResponse(Loan loan)
    {
        var schedulers = loan.LoanSchedulers?.ToList() ?? new List<LoanScheduler>();

        return new ActiveLoanResponse
        {
            LoanId = loan.Id,
            MemberId = loan.MemberId,
            FullName = (
                loan.Member.FirstName + " " +
                (loan.Member.MiddleName == null || loan.Member.MiddleName == ""
                    ? ""
                    : loan.Member.MiddleName + " ") +
                loan.Member.LastName).Trim(),
            Status = loan.Status,
            LoanTotalAmount = loan.TotalAmount,
            NoOfTerms =
                schedulers.Count.ToString() + "/" +
                schedulers.Count(s => string.Equals(s.Status, "Paid", StringComparison.OrdinalIgnoreCase)),
            TotalAmountPaid = schedulers
                .Where(s => string.Equals(s.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(s => s.PaymentAmount),
            SchedulerTotalAmount = schedulers.Sum(s => s.ActualEmiAmount),
            RemainingBal =
                schedulers.Sum(s => s.ActualEmiAmount) -
                schedulers
                    .Where(s => string.Equals(s.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                    .Sum(s => s.PaymentAmount),
        };
    }

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.Loans.AddAsync(loan, cancellationToken);
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .AsNoTracking()
            .Where(loan =>
                !loan.IsDeleted &&
                loan.Member.Center.BranchId == branchId &&
                (loan.Status.Trim().ToUpper() == "ACTIVE" ||
                 loan.Status.Trim().ToUpper() == "PENDING" ||
                 loan.Status.Trim().ToUpper() == "CLAIMED" ||
                 loan.Status.Trim().ToUpper() == "CLOSED"))
            .Select(loan => new ActiveLoanResponse
            {
                LoanId = loan.Id,
                MemberId = loan.MemberId,
                FullName = (
                    loan.Member.FirstName + " " +
                    (loan.Member.MiddleName == null || loan.Member.MiddleName == ""
                        ? ""
                        : loan.Member.MiddleName + " ") +
                    loan.Member.LastName).Trim(),
                Status = loan.Status,
                LoanTotalAmount = loan.TotalAmount,
                NoOfTerms =
                    loan.LoanSchedulers!.Count().ToString() + "/" +
                    loan.LoanSchedulers!.Count(scheduler => scheduler.Status == "Paid").ToString(),
                TotalAmountPaid = loan.LoanSchedulers!
                    .Where(scheduler => scheduler.Status == "Paid")
                    .Sum(scheduler => scheduler.PaymentAmount),
                SchedulerTotalAmount = loan.LoanSchedulers!
                    .Sum(scheduler => scheduler.ActualEmiAmount),
                RemainingBal =
                    loan.LoanSchedulers!.Sum(scheduler => scheduler.ActualEmiAmount) -
                    loan.LoanSchedulers!
                        .Where(scheduler => scheduler.Status == "Paid")
                        .Sum(scheduler => scheduler.PaymentAmount),
            })
            .OrderBy(loan => loan.LoanId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenSchedulersAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .AnyAsync(
                ls => ls.LoanId == loanId && ls.Status != "Paid",
                cancellationToken);
    }

    public async Task<bool> HasOpenLoanForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans.AnyAsync(
            loan => loan.MemberId == memberId &&
                    !loan.IsDeleted &&
                    (loan.Status == "Active" || loan.Status == "Defaulted"),
            cancellationToken);
    }
}