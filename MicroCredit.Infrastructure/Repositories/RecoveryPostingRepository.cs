using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Entities;
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
            join p in _context.POCs on m.POCId equals p.Id
            join c in _context.Centers on m.CenterId equals c.Id
            join b in _context.Branches on c.BranchId equals b.Id
            where ls.ScheduleDate >= dayStart
                  && ls.ScheduleDate < dayEnd
                  && l.Status == "Active"
                  && !l.IsDeleted
                  && !m.IsDeleted
                  && !p.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && b.OrgId == orgId
                  && b.Id == branchId
                  && ls.ParentLoanSchedulerId == null
                  && ls.SubInstallmentSequence == 0
                  && ls.Status == LoanSchedulerStatus.NotPaid // Only include Not Paid bases
                  && (!centerId.HasValue || c.Id == centerId.Value)
                  // Filter by the member's assigned POC (not merely "POC exists in center").
                  && (!pocId.HasValue || m.POCId == pocId.Value)
            select new RecoveryPostingSchedulerResponse
            {
                LoanId = l.Id,
                MemberId = l.MemberId,
                MemberCode = m.MemberCode,
                MemberName = ((m.FirstName ?? "") + " " + (m.MiddleName ?? "") + " " + (m.LastName ?? "")).Trim(),
                PocName = ((p.FirstName ?? "") + " " + (p.MiddleName ?? "") + " " + (p.LastName ?? "")).Trim(),
                LoanStatus = l.Status,
                LoanSchedulerId = ls.LoanSchedulerId,
                SchedulerLoanId = ls.LoanId,
                InstallmentNo = ls.InstallmentNo,
                ParentLoanSchedulerId = ls.ParentLoanSchedulerId,
                SubInstallmentSequence = ls.SubInstallmentSequence,
                ScheduleDate = ls.ScheduleDate,
                PaymentDate = ls.PaymentDate,
                ActualEmiAmount = ls.ActualEmiAmount,
                ActualPrincipalAmount = ls.ActualPrincipalAmount,
                ActualInterestAmount = ls.ActualInterestAmount,
                PaymentAmount = ls.PaymentAmount,
                InterestAmount = ls.InterestAmount,
                PrincipalAmount = ls.PrincipalAmount,
                SchedulerStatus = ls.Status == LoanSchedulerStatus.NotPaid ? "Not Paid"
                    : ls.Status == LoanSchedulerStatus.Paid ? "Paid"
                    : ls.Status == LoanSchedulerStatus.Partial ? "Partial"
                    : ls.Status == LoanSchedulerStatus.Claimed ? "Claimed"
                    : ls.Status == LoanSchedulerStatus.Overdue ? "Overdue"
                    : "Not Paid",
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
                  && ls.ParentLoanSchedulerId == null
                  && ls.SubInstallmentSequence == 0
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
                Status = ls.Status == LoanSchedulerStatus.NotPaid ? "Not Paid"
                    : ls.Status == LoanSchedulerStatus.Paid ? "Paid"
                    : ls.Status == LoanSchedulerStatus.Partial ? "Partial"
                    : ls.Status == LoanSchedulerStatus.Claimed ? "Claimed"
                    : ls.Status == LoanSchedulerStatus.Overdue ? "Overdue"
                    : "Not Paid",
                ActualEmiAmount = ls.ActualEmiAmount,
                ActualPrincipalAmount = ls.ActualPrincipalAmount,
                ActualInterestAmount = ls.ActualInterestAmount,
                ParentLoanSchedulerId = ls.ParentLoanSchedulerId,
                SubInstallmentSequence = ls.SubInstallmentSequence,
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
                && ls.Status == LoanSchedulerStatus.NotPaid)
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
                    .SetProperty(ls => ls.Status, LoanSchedulerStatus.Paid),
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
        // Legacy same-row Partial + next-EMI shortfall carry is retired.
        // PostRecoveriesAsync must use TryShrinkBaseForPartialAsync + InsertPaymentChildAsync.
        await Task.CompletedTask;
        throw new NotSupportedException(
            $"ApplyPartialRecoveryPaymentAsync is retired (LoanScheduler {loanSchedulerId}). " +
            "Use shrink-base + payment-child partial posting.");
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
                    .SetProperty(ls => ls.Status, LoanSchedulerStatus.Overdue),
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

    public async Task<string?> GetLoanCollectionTermAsync(
        int loanId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .AsNoTracking()
            .Where(l => l.Id == loanId && !l.IsDeleted)
            .Select(l => l.CollectionTerm)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateNextCarryForwardScheduleAsync(
        int loanId,
        DateTime scheduleDate,
        decimal actualPrincipalAmount,
        decimal actualInterestAmount,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var maxInstallmentNo = await _context.LoanSchedulers
            .Where(ls => ls.LoanId == loanId)
            .Select(ls => (int?)ls.InstallmentNo)
            .MaxAsync(cancellationToken) ?? 0;

        var principal = Math.Round(actualPrincipalAmount, 2);
        var interest = Math.Round(actualInterestAmount, 2);
        var schedule = new LoanScheduler(
            loanId: loanId,
            scheduleDate: scheduleDate.Date,
            paymentAmount: 0,
            principalAmount: 0,
            interestAmount: 0,
            installmentNo: maxInstallmentNo + 1,
            createdBy: createdBy,
            actualEmiAmount: Math.Round(principal + interest, 2),
            actualPrincipalAmount: principal,
            actualInterestAmount: interest,
            savingAmount: 0);

        await _context.LoanSchedulers.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var loanRows = await _context.Loans
            .Where(l => l.Id == loanId && !l.IsDeleted)
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.NoOfTerms, l => l.NoOfTerms + 1),
                cancellationToken);

        if (loanRows == 0)
            throw new InvalidOperationException($"Loan {loanId} could not be updated when creating next installment.");

        if (schedule.LoanSchedulerId <= 0)
            throw new InvalidOperationException($"Failed to create next installment for Loan {loanId}.");

        return schedule.LoanSchedulerId;
    }

    public async Task<LoanScheduler?> LockAndGetBaseSchedulerAsync(
        int loanSchedulerId,
        CancellationToken cancellationToken = default)
    {
        // UPDLOCK + ROWLOCK so concurrent posts serialize on the same base row.
        return await _context.LoanSchedulers
            .FromSqlInterpolated($@"
SELECT *
FROM [dinspire_sa].[LoanSchedulers] WITH (UPDLOCK, ROWLOCK)
WHERE [LoanSchedulerId] = {loanSchedulerId}")
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> TryShrinkBaseForPartialAsync(
        int loanSchedulerId,
        decimal expectedActualEmi,
        decimal expectedActualPrincipal,
        decimal expectedActualInterest,
        decimal remainingEmi,
        decimal remainingPrincipal,
        decimal remainingInterest,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .Where(ls =>
                ls.LoanSchedulerId == loanSchedulerId
                && ls.Status == LoanSchedulerStatus.NotPaid
                && ls.ActualEmiAmount == expectedActualEmi
                && ls.ActualPrincipalAmount == expectedActualPrincipal
                && ls.ActualInterestAmount == expectedActualInterest)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.PaymentDate, (DateTime?)null)
                    .SetProperty(ls => ls.PaymentAmount, 0m)
                    .SetProperty(ls => ls.PrincipalAmount, 0m)
                    .SetProperty(ls => ls.InterestAmount, 0m)
                    .SetProperty(ls => ls.PaymentMode, (string?)null)
                    .SetProperty(ls => ls.CollectedBy, (int?)null)
                    .SetProperty(ls => ls.Comments, (string?)null)
                    .SetProperty(ls => ls.ActualEmiAmount, remainingEmi)
                    .SetProperty(ls => ls.ActualPrincipalAmount, remainingPrincipal)
                    .SetProperty(ls => ls.ActualInterestAmount, remainingInterest)
                    .SetProperty(ls => ls.Status, LoanSchedulerStatus.NotPaid),
                cancellationToken);
    }

    public async Task<int> TryCloseBaseAfterFinalSettleAsync(
        int loanSchedulerId,
        decimal expectedActualEmi,
        decimal expectedActualPrincipal,
        decimal expectedActualInterest,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .Where(ls =>
                ls.LoanSchedulerId == loanSchedulerId
                && ls.Status == LoanSchedulerStatus.NotPaid
                && ls.ActualEmiAmount == expectedActualEmi
                && ls.ActualPrincipalAmount == expectedActualPrincipal
                && ls.ActualInterestAmount == expectedActualInterest)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(ls => ls.PaymentDate, (DateTime?)null)
                    .SetProperty(ls => ls.PaymentAmount, 0m)
                    .SetProperty(ls => ls.PrincipalAmount, 0m)
                    .SetProperty(ls => ls.InterestAmount, 0m)
                    .SetProperty(ls => ls.PaymentMode, (string?)null)
                    .SetProperty(ls => ls.CollectedBy, (int?)null)
                    .SetProperty(ls => ls.Comments, (string?)null)
                    .SetProperty(ls => ls.ActualEmiAmount, 0m)
                    .SetProperty(ls => ls.ActualPrincipalAmount, 0m)
                    .SetProperty(ls => ls.ActualInterestAmount, 0m)
                    .SetProperty(ls => ls.Status, LoanSchedulerStatus.Paid),
                cancellationToken);
    }

    public async Task<int> GetNextSubInstallmentSequenceAsync(
        int parentLoanSchedulerId,
        CancellationToken cancellationToken = default)
    {
        var maxSeq = await _context.LoanSchedulers
            .Where(ls => ls.ParentLoanSchedulerId == parentLoanSchedulerId)
            .Select(ls => (int?)ls.SubInstallmentSequence)
            .MaxAsync(cancellationToken) ?? 0;

        return maxSeq + 1;
    }

    public async Task<bool> HasPaymentChildrenAsync(
        int baseLoanSchedulerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .AnyAsync(ls => ls.ParentLoanSchedulerId == baseLoanSchedulerId, cancellationToken);
    }

    public async Task<int> InsertPaymentChildAsync(
        LoanScheduler child,
        CancellationToken cancellationToken = default)
    {
        await _context.LoanSchedulers.AddAsync(child, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return child.LoanSchedulerId;
    }

    public async Task<int> CountBlockingEarlierBasesAsync(
        int loanId,
        int beforeInstallmentNo,
        CancellationToken cancellationToken = default)
    {
        // Not Paid with remaining Actual blocks. Overdue blocks only when untransferred:
        // missing PaymentDate (not via overdue post path) or no later base carry destination.
        // Successful overdue posts set PaymentDate and carry in the same transaction.
        return await _context.LoanSchedulers
            .CountAsync(
                ls =>
                    ls.LoanId == loanId
                    && ls.ParentLoanSchedulerId == null
                    && ls.SubInstallmentSequence == 0
                    && ls.InstallmentNo < beforeInstallmentNo
                    && (
                        (ls.Status == LoanSchedulerStatus.NotPaid && ls.ActualEmiAmount > 0)
                        || (
                            ls.Status == LoanSchedulerStatus.Overdue
                            && (
                                ls.PaymentDate == null
                                || !_context.LoanSchedulers.Any(n =>
                                    n.LoanId == loanId
                                    && n.ParentLoanSchedulerId == null
                                    && n.SubInstallmentSequence == 0
                                    && n.InstallmentNo > ls.InstallmentNo)))),
                cancellationToken);
    }
}
