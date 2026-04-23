using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.RecoveryPosting;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class RecoveryPostingRepository : IRecoveryPostingRepository
{
    private readonly MicroCreditDbContext _context;

    public RecoveryPostingRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RecoveryPostingSchedulerResponse>> GetSchedulersAsync(
        DateTime scheduleDate,
        int orgId,
        int branchId,
        int? centerId,
        int? pocId,
        CancellationToken cancellationToken = default)
    {
        var dayStart = scheduleDate.Date;
        var dayEnd = dayStart.AddDays(1);

        var query =
            from ls in _context.LoanSchedulers
            join l in _context.Loans on ls.LoanId equals l.Id
            join m in _context.Members on l.MemberId equals m.Id
            join c in _context.Centers on m.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where ls.ScheduleDate >= dayStart
                  && ls.ScheduleDate < dayEnd
                  && l.Status == "Active"
                  && !l.IsDeleted
                  && !m.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
                  && ls.Status == "Not Paid" // Only include Not Paid
                  && (!centerId.HasValue || c.Id == centerId.Value)
                  && (!pocId.HasValue
                      || _context.POCs.Any(p =>
                          p.Id == pocId.Value && p.CenterId == c.Id && !p.IsDeleted))
            select new RecoveryPostingSchedulerResponse
            {
                LoanId = l.Id,
                MemberId = l.MemberId,
                LoanStatus = l.Status,
                LoanSchedulerId = ls.LoanSchedulerId,
                SchedulerLoanId = ls.LoanId,
                InstallmentNo = ls.InstallmentNo,
                ScheduleDate = ls.ScheduleDate,
                PaymentDate = ls.PaymentDate,
                ActualEmiAmount = ls.ActualEmiAmount,
                ActualPrincipalAmount = ls.ActualPrincipalAmount,
                ActualInterestAmount = ls.ActualInterestAmount,
                PaymentAmount = ls.PaymentAmount,
                InterestAmount = ls.InterestAmount,
                PrincipalAmount = ls.PrincipalAmount,
                SchedulerStatus = ls.Status,
                PaymentMode = ls.PaymentMode,
                CollectedBy = ls.CollectedBy,
                Comments = ls.Comments,
                CreatedBy = ls.CreatedBy,
                CreatedDate = ls.CreatedDate,
                CenterId = c.Id,
                CenterName = c.Name,
                BranchId = b.Id,
                BranchName = b.Name
            };

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecoveryPostingSchedulerSnapshot>> GetLoanSchedulerSnapshotsForBranchByIdsAsync(
        IReadOnlyCollection<int> loanSchedulerIds,
        int orgId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        if (loanSchedulerIds == null || loanSchedulerIds.Count == 0)
            return Array.Empty<RecoveryPostingSchedulerSnapshot>();

        var query =
            from ls in _context.LoanSchedulers
            join l in _context.Loans on ls.LoanId equals l.Id
            join m in _context.Members on l.MemberId equals m.Id
            join c in _context.Centers on m.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where loanSchedulerIds.Contains(ls.LoanSchedulerId)
                  && l.Status == "Active"
                  && !l.IsDeleted
                  && !m.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
            select new RecoveryPostingSchedulerSnapshot
            {
                LoanSchedulerId = ls.LoanSchedulerId,
                LoanId = ls.LoanId,
                InstallmentNo = ls.InstallmentNo,
                ScheduleDate = ls.ScheduleDate,
                Status = ls.Status,
                ActualEmiAmount = ls.ActualEmiAmount,
                ActualPrincipalAmount = ls.ActualPrincipalAmount,
                ActualInterestAmount = ls.ActualInterestAmount,
            };

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetNextUnpaidLoanSchedulerIdAsync(
        int loanId,
        int afterInstallmentNo,
        CancellationToken cancellationToken = default)
    {
        var id = await _context.LoanSchedulers
            .Where(ls =>
                ls.LoanId == loanId
                && ls.InstallmentNo > afterInstallmentNo
                && ls.Status == "Not Paid")
            .OrderBy(ls => ls.InstallmentNo)
            .Select(ls => ls.LoanSchedulerId)
            .FirstOrDefaultAsync(cancellationToken);

        return id == 0 ? null : id;
    }

    public async Task ApplyFullRecoveryPaymentAsync(
        int loanSchedulerId,
        decimal paymentEmi,
        decimal principalPaid,
        decimal interestPaid,
        int collectedBy,
        string? paymentMode,
        string? comments,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.LoanSchedulers
            .Where(ls => ls.LoanSchedulerId == loanSchedulerId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.PaymentDate, now)
                    .SetProperty(ls => ls.PaymentAmount, paymentEmi)
                    .SetProperty(ls => ls.PrincipalAmount, principalPaid)
                    .SetProperty(ls => ls.InterestAmount, interestPaid)
                    .SetProperty(ls => ls.ActualEmiAmount, paymentEmi)
                    .SetProperty(ls => ls.ActualPrincipalAmount, principalPaid)
                    .SetProperty(ls => ls.ActualInterestAmount, interestPaid)
                    .SetProperty(ls => ls.CollectedBy, collectedBy)
                    .SetProperty(ls => ls.PaymentMode, paymentMode)
                    .SetProperty(ls => ls.Comments, comments)
                    .SetProperty(ls => ls.Status, "Paid"),
                cancellationToken);

        if (rows == 0)
            throw new InvalidOperationException($"LoanScheduler {loanSchedulerId} could not be updated.");
    }

    public async Task ApplyPartialRecoveryPaymentAsync(
        int loanSchedulerId,
        decimal amountPaid,
        decimal principalPaid,
        decimal interestPaid,
        int collectedBy,
        string? paymentMode,
        string? comments,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.LoanSchedulers
            .Where(ls => ls.LoanSchedulerId == loanSchedulerId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.PaymentDate, now)
                    .SetProperty(ls => ls.PaymentAmount, amountPaid)
                    .SetProperty(ls => ls.PrincipalAmount, principalPaid)
                    .SetProperty(ls => ls.InterestAmount, interestPaid)
                    .SetProperty(ls => ls.CollectedBy, collectedBy)
                    .SetProperty(ls => ls.PaymentMode, paymentMode)
                    .SetProperty(ls => ls.Comments, comments)
                    .SetProperty(ls => ls.Status, "Partial"),
                cancellationToken);

        if (rows == 0)
            throw new InvalidOperationException($"LoanScheduler {loanSchedulerId} could not be updated.");
    }

    public async Task ApplyOverdueRecoveryAsync(
        int loanSchedulerId,
        int collectedBy,
        string? comments,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.LoanSchedulers
            .Where(ls => ls.LoanSchedulerId == loanSchedulerId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.PaymentDate, now)
                    .SetProperty(ls => ls.CollectedBy, collectedBy)
                    .SetProperty(ls => ls.Comments, comments)
                    .SetProperty(ls => ls.Status, "Overdue"),
                cancellationToken);

        if (rows == 0)
            throw new InvalidOperationException($"LoanScheduler {loanSchedulerId} could not be updated.");
    }

    public async Task AddCarryForwardToScheduleAsync(
        int loanSchedulerId,
        decimal shortfallPrincipal,
        decimal shortfallInterest,
        CancellationToken cancellationToken = default)
    {
        var sp = shortfallPrincipal;
        var si = shortfallInterest;
        var rows = await _context.LoanSchedulers
            .Where(ls => ls.LoanSchedulerId == loanSchedulerId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.ActualPrincipalAmount, ls => ls.ActualPrincipalAmount + sp)
                    .SetProperty(ls => ls.ActualInterestAmount, ls => ls.ActualInterestAmount + si)
                    .SetProperty(
                        ls => ls.ActualEmiAmount,
                        ls => ls.ActualPrincipalAmount + sp + ls.ActualInterestAmount + si),
                cancellationToken);

        if (rows == 0)
            throw new InvalidOperationException($"LoanScheduler {loanSchedulerId} could not be updated for carry-forward.");
    }
}
