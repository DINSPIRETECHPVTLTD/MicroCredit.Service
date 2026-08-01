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
                  && ls.Status == LoanSchedulerStatus.NotPaid
                  && (!centerId.HasValue || c.Id == centerId.Value)
                  && (!pocId.HasValue || m.POCId == pocId.Value)
            orderby ls.InstallmentNo, ls.SubInstallmentSequence
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
                SubInstallmentSequence = ls.SubInstallmentSequence,
                ParentLoanSchedulerId = ls.ParentLoanSchedulerId,
                InstallmentLabel = ls.SubInstallmentSequence > 0
                    ? ls.InstallmentNo.ToString() + "_" + ls.SubInstallmentSequence.ToString()
                    : ls.InstallmentNo.ToString(),
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
                SubInstallmentSequence = ls.SubInstallmentSequence,
                ParentLoanSchedulerId = ls.ParentLoanSchedulerId,
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
                SavingAmount = ls.SavingAmount,
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
                && ls.SubInstallmentSequence == 0
                && ls.InstallmentNo > afterInstallmentNo
                && ls.Status == LoanSchedulerStatus.NotPaid)
            .OrderBy(ls => ls.InstallmentNo)
            .ThenBy(ls => ls.SubInstallmentSequence)
            .Select(ls => ls.LoanSchedulerId)
            .FirstOrDefaultAsync(cancellationToken);

        return id == 0 ? null : id;
    }

    public async Task<int> GetNextSubInstallmentSequenceAsync(
        int loanId,
        int installmentNo,
        CancellationToken cancellationToken = default)
    {
        var maxSequence = await _context.LoanSchedulers
            .Where(ls => ls.LoanId == loanId && ls.InstallmentNo == installmentNo)
            .MaxAsync(ls => (int?)ls.SubInstallmentSequence, cancellationToken);

        return (maxSequence ?? 0) + 1;
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
        decimal actualEmiPaid,
        decimal actualPrincipalPaid,
        decimal actualInterestPaid,
        decimal? savingPaid,
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
                    .SetProperty(ls => ls.ActualEmiAmount, actualEmiPaid)
                    .SetProperty(ls => ls.ActualPrincipalAmount, actualPrincipalPaid)
                    .SetProperty(ls => ls.ActualInterestAmount, actualInterestPaid)
                    .SetProperty(ls => ls.SavingAmount, savingPaid)
                    .SetProperty(ls => ls.CollectedBy, collectedBy)
                    .SetProperty(ls => ls.PaymentMode, paymentMode)
                    .SetProperty(ls => ls.Comments, comments)
                    .SetProperty(ls => ls.Status, LoanSchedulerStatus.Partial),
                cancellationToken);

        if (rows == 0)
            throw new InvalidOperationException($"LoanScheduler {loanSchedulerId} could not be updated.");
    }

    public async Task<int> CreatePartialRemainderSchedulerAsync(
        int loanId,
        DateTime scheduleDate,
        int installmentNo,
        int subInstallmentSequence,
        int parentLoanSchedulerId,
        int createdBy,
        decimal actualEmiAmount,
        decimal actualPrincipalAmount,
        decimal actualInterestAmount,
        decimal? savingAmount,
        CancellationToken cancellationToken = default)
    {
        var schedule = LoanScheduler.CreatePartialRemainder(
            loanId,
            scheduleDate,
            installmentNo,
            subInstallmentSequence,
            parentLoanSchedulerId,
            createdBy,
            actualEmiAmount,
            actualPrincipalAmount,
            actualInterestAmount,
            savingAmount);

        await _context.LoanSchedulers.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return schedule.LoanSchedulerId;
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
}
