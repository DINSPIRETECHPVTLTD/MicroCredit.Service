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
        var loans = await _context.Loans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(l => l.Member)
                .ThenInclude(m => m.POC)
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
            MemberCode = loan.Member?.MemberCode,
            FullName = (
                loan.Member?.FirstName + " " +
                (string.IsNullOrEmpty(loan.Member?.MiddleName)
                    ? ""
                    : loan.Member.MiddleName + " ") +
                loan.Member?.LastName).Trim(),
            PocName = (
                loan.Member?.POC?.FirstName + " " +
                (string.IsNullOrEmpty(loan.Member?.POC?.MiddleName)
                    ? ""
                    : loan.Member.POC.MiddleName + " ") +
                loan.Member?.POC?.LastName).Trim(),
            Status = loan.Status,
            LoanTotalAmount = loan.TotalAmount,
            NoOfTerms = LoanSchedulerSummaryCalculator.FormatTermsProgress(schedulers),
            TotalAmountPaid = LoanSchedulerSummaryCalculator.TotalAmountPaid(schedulers),
            SchedulerTotalAmount = LoanSchedulerSummaryCalculator.SchedulerTotalAmount(schedulers),
            RemainingBal = LoanSchedulerSummaryCalculator.RemainingBalance(schedulers),
        };
    }

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.Loans.AddAsync(loan, cancellationToken);
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(loan =>
                !loan.IsDeleted &&
                loan.Member.Center.BranchId == branchId &&
                (loan.Status.Trim().ToUpper() == "ACTIVE" ||
                 loan.Status.Trim().ToUpper() == "PENDING" ||
                 loan.Status.Trim().ToUpper() == "CLAIMED" ||
                 loan.Status.Trim().ToUpper() == "CLOSED"))
            .Include(loan => loan.LoanSchedulers)
            .Include(loan => loan.Member)
                .ThenInclude(m => m.POC)
            .OrderBy(loan => loan.Id)
            .ToListAsync(cancellationToken);

        return loans.Select(MapLoanToActiveLoanResponse).ToList();
    }

    public async Task<bool> HasOpenSchedulersAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .AnyAsync(
                ls => ls.LoanId == loanId && ls.Status != LoanSchedulerStatus.Paid,
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

    public async Task<Dictionary<int, int>> GetMaxViewableLoanIdsByMemberIdsAsync(
        IReadOnlyList<int> memberIds,
        CancellationToken cancellationToken = default)
    {
        if (memberIds == null || memberIds.Count == 0)
            return new Dictionary<int, int>();

        var distinctIds = memberIds.Distinct().ToList();

        var rows = await _context.Loans
            .AsNoTracking()
            .Where(l => !l.IsDeleted && distinctIds.Contains(l.MemberId))
            .Where(l =>
                l.Status == "Active" || l.Status == "Pending" || l.Status == "Claimed" ||
                l.Status == "ACTIVE" || l.Status == "PENDING" || l.Status == "CLAIMED")
            .Select(l => new { l.MemberId, l.Id })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.MemberId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Id));
    }
}
