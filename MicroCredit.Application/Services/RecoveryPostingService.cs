using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.RecoveryPosting;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroCredit.Application.Services;

public class RecoveryPostingService : IRecoveryPostingService
{
    private const string EmiRecoveryTransactionType = "EMI Recovery";
    private const string StatusNotPaid = "Not Paid";
    private const string StatusPaid = "Paid";
    private const string StatusPartialPaid = "Partial Paid";
    private const string StatusPartial = "Partial";
    private const string StatusOverdue = "Overdue";

    private readonly IUnitOfWork _unitOfWork;  // ✅ CHANGED: Use UnitOfWork instead of direct repository
    private readonly ILedgerRecordService _ledgerRecordService;
    private readonly MicroCreditDbContext _db;
    private readonly ILogger<RecoveryPostingService> _logger;

    public RecoveryPostingService(
        IUnitOfWork unitOfWork,  // ✅ CHANGED: Inject UnitOfWork
        ILedgerRecordService ledgerRecordService,
        MicroCreditDbContext db,
        ILogger<RecoveryPostingService> logger)
    {
        _unitOfWork = unitOfWork;
        _ledgerRecordService = ledgerRecordService;
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecoveryPostingSchedulerResponse>> GetSchedulersAsync(
        RecoveryPostingSchedulersRequest request,
        IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ScheduleDate == default)
            throw new ArgumentException("ScheduleDate is required.", nameof(request));

        if (!userContext.BranchId.HasValue)
            throw new InvalidOperationException("Branch context is required to load recovery posting schedules.");

        _logger.LogInformation(
            "Recovery posting schedulers: ScheduleDate={ScheduleDate}, CenterId={CenterId}, POCId={POCId}, BranchId={BranchId}, OrgId={OrgId}",
            request.ScheduleDate.Date,
            request.CenterId,
            request.POCId,
            userContext.BranchId,
            userContext.OrgId);

        // ✅ CHANGED: Access repository through UnitOfWork
        return await _unitOfWork.RecoveryPostings.GetSchedulersAsync(
            request.ScheduleDate,
            userContext.OrgId,
            userContext.BranchId.Value,
            request.CenterId,
            request.POCId,
            cancellationToken);
    }

    public async Task<RecoveryPostingPostResponse> PostRecoveriesAsync(
        RecoveryPostingPostRequest request,
        IUserContext userContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException(
                "At least one recovery line item is required (LoanSchedulerId, amounts, PaymentMode, Status).",
                nameof(request));

        if (request.CollectedBy <= 0)
            throw new ArgumentException("CollectedBy is required (select staff who collected the payment).", nameof(request));

        if (!userContext.BranchId.HasValue)
            throw new InvalidOperationException("Branch context is required to post recovery.");

        foreach (var line in request.Items)
        {
            if (line.LoanSchedulerId <= 0)
                throw new ArgumentException("Each item must include a valid LoanSchedulerId.");

            if (string.IsNullOrWhiteSpace(line.Status))
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: Status is required (use \"Paid\", \"Partial Paid\", or \"Overdue\").");

            var normalizedStatus = NormalizePostedStatus(line.Status);
            if (normalizedStatus == null)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: Unsupported status \"{line.Status}\".");

            if (string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
            {
                var overduePayment = line.PaymentAmount ?? 0m;
                var overduePrincipal = line.PrincipalAmount ?? 0m;
                var overdueInterest = line.InterestAmount ?? 0m;
                if (overduePayment != 0m || overduePrincipal != 0m || overdueInterest != 0m)
                    throw new ArgumentException(
                        $"LoanScheduler {line.LoanSchedulerId}: Overdue requires PaymentAmount, PrincipalAmount, and InterestAmount to be zero or blank.");
                continue;
            }

            var payment = line.PaymentAmount ?? 0m;
            var principal = line.PrincipalAmount ?? 0m;
            var interest = line.InterestAmount ?? 0m;

            if (payment <= 0)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentAmount is required and must be greater than zero.");

            if (principal < 0 || interest < 0)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PrincipalAmount and InterestAmount cannot be negative.");

            if (string.IsNullOrWhiteSpace(line.PaymentMode))
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentMode is required unless status is Overdue.");
        }

        var distinctIds = request.Items.Select(i => i.LoanSchedulerId).Distinct().ToList();
        if (distinctIds.Count != request.Items.Count)
            throw new ArgumentException(
                "Duplicate LoanSchedulerId in the request: each installment row can only be posted once.");

        // ✅ CHANGED: Use UnitOfWork repository
        var snapshots = await _unitOfWork.RecoveryPostings.GetLoanSchedulerSnapshotsForBranchByIdsAsync(
            distinctIds,
            userContext.OrgId,
            userContext.BranchId.Value,
            cancellationToken);

        if (snapshots.Count != distinctIds.Count)
            throw new InvalidOperationException(
                "One or more installments were not found, or do not belong to this branch.");

        var ordered = snapshots
            .OrderBy(e => e.LoanId)
            .ThenBy(e => e.InstallmentNo)
            .ToList();

        var loanIds = ordered.Select(x => x.LoanId).Distinct().ToList();
        var loanMemberMap = await _db.Loans
            .AsNoTracking()
            .Where(l => loanIds.Contains(l.Id))
            .Select(l => new
            {
                l.Id,
                l.MemberId,
                MemberName = ((l.Member.FirstName ?? string.Empty) + " " +
                              (l.Member.MiddleName ?? string.Empty) + " " +
                              (l.Member.LastName ?? string.Empty)).Trim()
            })
            .ToDictionaryAsync(
                x => x.Id,
                x => new { x.MemberId, x.MemberName },
                cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var ledgerRecoveryTotalsByLoan = new Dictionary<int, (decimal TotalAmount, int Count)>();

            foreach (var row in ordered)
            {
                var line = request.Items.First(i => i.LoanSchedulerId == row.LoanSchedulerId);

                if (!string.Equals(row.Status, StatusNotPaid, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: this installment is not in \"Not Paid\" status and cannot be posted.");
                }

                var normalizedStatus = NormalizePostedStatus(line.Status);
                if (normalizedStatus == null)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: Status must be \"Paid\", \"Partial Paid\", or \"Overdue\".");
                }

                var payment = line.PaymentAmount ?? 0m;
                var pr = line.PrincipalAmount ?? 0m;
                var ir = line.InterestAmount ?? 0m;

                if (payment <= 0)
                {
                    if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount must be greater than zero.");
                    }
                }

                if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase) && payment != pr + ir)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: PrincipalAmount plus InterestAmount must equal PaymentAmount.");
                }

                var dueEmi = row.ActualEmiAmount;
                var dueP = row.ActualPrincipalAmount;
                var dueI = row.ActualInterestAmount;

                if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase) && dueEmi > 0 && payment > dueEmi)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount cannot exceed the scheduled EMI (ActualEmiAmount).");
                }

                if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase) && dueEmi <= 0 && payment > 0)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: scheduled EMI is zero; a payment cannot be posted.");
                }

                if (string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
                {
                    var today = DateTime.Today;
                    if (row.ScheduleDate.Date >= today)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: Overdue is allowed only after schedule date has passed.");
                    }

                    await _unitOfWork.RecoveryPostings.ApplyOverdueRecoveryAsync(
                        row.LoanSchedulerId,
                        request.CollectedBy,
                        line.Comments,
                        cancellationToken);

                    var nextId = await _unitOfWork.RecoveryPostings.GetNextUnpaidLoanSchedulerIdAsync(
                        row.LoanId,
                        row.InstallmentNo,
                        cancellationToken);

                    if (nextId == null)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: there is no next unpaid installment to carry overdue amount to.");
                    }

                    await _unitOfWork.RecoveryPostings.AddCarryForwardToScheduleAsync(
                        nextId.Value,
                        dueP,
                        dueI,
                        cancellationToken);
                }
                else
                {
                    var isFullPayment = dueEmi > 0 && payment >= dueEmi;
                    if (!PostedStatusMatchesPayment(normalizedStatus, isFullPayment))
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: Status must be \"Paid\" when the payment covers the full scheduled EMI, or \"Partial Paid\" when it is a partial payment.");
                    }

                    if (isFullPayment)
                    {
                        await _unitOfWork.RecoveryPostings.ApplyFullRecoveryPaymentAsync(
                            row.LoanSchedulerId,
                            payment,
                            pr,
                            ir,
                            request.CollectedBy,
                            line.PaymentMode,
                            line.Comments,
                            cancellationToken);
                    }
                    else
                    {
                        await _unitOfWork.RecoveryPostings.ApplyPartialRecoveryPaymentAsync(
                            row.LoanSchedulerId,
                            payment,
                            pr,
                            ir,
                            request.CollectedBy,
                            line.PaymentMode,
                            line.Comments,
                            cancellationToken);

                        var shortfallP = dueP - pr;
                        var shortfallI = dueI - ir;
                        if (shortfallP < 0 || shortfallI < 0)
                        {
                            throw new InvalidOperationException(
                                $"LoanScheduler {row.LoanSchedulerId}: PrincipalAmount and InterestAmount do not match the scheduled split for a partial payment.");
                        }

                        var nextId = await _unitOfWork.RecoveryPostings.GetNextUnpaidLoanSchedulerIdAsync(
                            row.LoanId,
                            row.InstallmentNo,
                            cancellationToken);

                        if (nextId == null)
                        {
                            throw new InvalidOperationException(
                                $"LoanScheduler {row.LoanSchedulerId}: there is no next unpaid installment to carry the shortfall to.");
                        }

                        await _unitOfWork.RecoveryPostings.AddCarryForwardToScheduleAsync(
                            nextId.Value,
                            shortfallP,
                            shortfallI,
                            cancellationToken);
                    }
                }

                if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
                {
                    if (ledgerRecoveryTotalsByLoan.TryGetValue(row.LoanId, out var agg))
                    {
                        ledgerRecoveryTotalsByLoan[row.LoanId] = (agg.TotalAmount + payment, agg.Count + 1);
                    }
                    else
                    {
                        ledgerRecoveryTotalsByLoan[row.LoanId] = (payment, 1);
                    }
                }
            }

            if (!request.SkipLedgerTransaction)
            {
                // Create one ledger transaction per loan with summed posted EMI amount.
                foreach (var kvp in ledgerRecoveryTotalsByLoan)
                {
                    var loanId = kvp.Key;
                    var totalAmount = kvp.Value.TotalAmount;
                    var count = kvp.Value.Count;
                    if (totalAmount <= 0) continue;

                    var memberInfo = loanMemberMap.TryGetValue(loanId, out var v) ? v : null;
                    var memberLabel = memberInfo?.MemberName;
                    if (string.IsNullOrWhiteSpace(memberLabel))
                    {
                        memberLabel = memberInfo != null ? memberInfo.MemberId.ToString() : "Unknown";
                    }

                    var comment = count == 1
                        ? $"EMI recovery posted for Loan ID: {loanId}, Member ID: {memberLabel}."
                        : $"EMI recovery posted for Loan ID: {loanId}, Member ID: {memberLabel}. Total from {count} EMI rows.";

                    await _ledgerRecordService.RecordDepositAsync(
                        paidToUserId: request.CollectedBy,
                        amount: totalAmount,
                        paymentDate: DateTime.UtcNow,
                        createdBy: userContext.UserId,
                        createdDate: DateTime.UtcNow,
                        transactionType: EmiRecoveryTransactionType,
                        referenceId: loanId,
                        comments: comment,
                        cancellationToken: cancellationToken);
                }
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation(
            "Recovery posting post: PostedCount={Count}, BranchId={BranchId}, OrgId={OrgId}",
            ordered.Count,
            userContext.BranchId,
            userContext.OrgId);

        return new RecoveryPostingPostResponse
        {
            PostedCount = ordered.Count,
            Message = ordered.Count == 1
                ? "Recovery posting completed successfully for 1 installment."
                : $"Recovery posting completed successfully for {ordered.Count} installments."
        };
    }

    /// <summary>Client-sent Status must align with full vs partial payment (same meaning as UI labels).</summary>
    private static bool PostedStatusMatchesPayment(string? status, bool isFullPayment)
    {
        var normalizedStatus = NormalizePostedStatus(status);
        if (normalizedStatus == null)
            return false;

        if (isFullPayment)
            return string.Equals(normalizedStatus, StatusPaid, StringComparison.OrdinalIgnoreCase);

        return string.Equals(normalizedStatus, StatusPartialPaid, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizePostedStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var s = status.Trim();
        if (string.Equals(s, StatusPaid, StringComparison.OrdinalIgnoreCase))
            return StatusPaid;
        if (string.Equals(s, StatusPartialPaid, StringComparison.OrdinalIgnoreCase)
            || string.Equals(s, StatusPartial, StringComparison.OrdinalIgnoreCase))
            return StatusPartialPaid;
        if (string.Equals(s, StatusOverdue, StringComparison.OrdinalIgnoreCase))
            return StatusOverdue;
        return null;
    }
}
