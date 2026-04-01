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

            if (line.PaymentAmount <= 0)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentAmount is required and must be greater than zero.");

            if (line.PrincipalAmount < 0 || line.InterestAmount < 0)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PrincipalAmount and InterestAmount cannot be negative.");

            if (string.IsNullOrWhiteSpace(line.PaymentMode))
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentMode is required.");

            if (string.IsNullOrWhiteSpace(line.Status))
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: Status is required (use \"Paid\" or \"Partial Paid\").");
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
            foreach (var row in ordered)
            {
                var line = request.Items.First(i => i.LoanSchedulerId == row.LoanSchedulerId);

                if (!string.Equals(row.Status, "Not Paid", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: this installment is not in \"Not Paid\" status and cannot be posted.");
                }

                var payment = line.PaymentAmount;
                var pr = line.PrincipalAmount;
                var ir = line.InterestAmount;

                if (payment <= 0)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount must be greater than zero.");
                }

                if (payment != pr + ir)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: PrincipalAmount plus InterestAmount must equal PaymentAmount.");
                }

                var dueEmi = row.ActualEmiAmount;
                var dueP = row.ActualPrincipalAmount;
                var dueI = row.ActualInterestAmount;

                if (dueEmi > 0 && payment > dueEmi)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount cannot exceed the scheduled EMI (ActualEmiAmount).");
                }

                if (dueEmi <= 0 && payment > 0)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: scheduled EMI is zero; a payment cannot be posted.");
                }

                var isFullPayment = dueEmi > 0 && payment >= dueEmi;

                if (!PostedStatusMatchesPayment(line.Status, isFullPayment))
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: Status must be \"Paid\" when the payment covers the full scheduled EMI, or \"Partial Paid\" when it is a partial payment.");
                }

                if (isFullPayment)
                {
                    // ✅ CHANGED: Use UnitOfWork repository
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
                    // ✅ CHANGED: Use UnitOfWork repository
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

                    // ✅ CHANGED: Use UnitOfWork repository
                    var nextId = await _unitOfWork.RecoveryPostings.GetNextUnpaidLoanSchedulerIdAsync(
                        row.LoanId,
                        row.InstallmentNo,
                        cancellationToken);

                    if (nextId == null)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: there is no next unpaid installment to carry the shortfall to.");
                    }

                    // ✅ CHANGED: Use UnitOfWork repository
                    await _unitOfWork.RecoveryPostings.AddCarryForwardToScheduleAsync(
                        nextId.Value,
                        shortfallP,
                        shortfallI,
                        cancellationToken);
                }

                // Recovery collection should credit ledger balance for the collector.
                var alreadyRecorded = await _unitOfWork.LedgerTransaction.ExistsByTypeAndReferenceIdAsync(
                    EmiRecoveryTransactionType,
                    row.LoanSchedulerId,
                    cancellationToken);

                if (!alreadyRecorded)
                {
                    var memberInfo = loanMemberMap.TryGetValue(row.LoanId, out var v) ? v : null;
                    var memberLabel = memberInfo?.MemberName;
                    if (string.IsNullOrWhiteSpace(memberLabel))
                    {
                        memberLabel = memberInfo != null ? memberInfo.MemberId.ToString() : "Unknown";
                    }

                    var defaultComment =
                        $"Loan Payment for Loan ID: {row.LoanId}, Loan Scheduler: {row.LoanSchedulerId}, Member ID: {memberLabel}";
                    var finalComment = string.IsNullOrWhiteSpace(line.Comments)
                        ? defaultComment
                        : $"{defaultComment} | Reason: {line.Comments.Trim()}";

                    await _ledgerRecordService.RecordDepositAsync(
                        paidToUserId: request.CollectedBy,
                        amount: payment,
                        paymentDate: DateTime.UtcNow,
                        createdBy: userContext.UserId,
                        createdDate: DateTime.UtcNow,
                        transactionType: EmiRecoveryTransactionType,
                        referenceId: row.LoanSchedulerId,
                        comments: finalComment,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "Skipping duplicate EMI Recovery ledger entry for LoanSchedulerId={LoanSchedulerId}.",
                        row.LoanSchedulerId);
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
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var s = status.Trim();
        if (isFullPayment)
            return string.Equals(s, "Paid", StringComparison.OrdinalIgnoreCase);

        return string.Equals(s, "Partial Paid", StringComparison.OrdinalIgnoreCase)
               || string.Equals(s, "Partial", StringComparison.OrdinalIgnoreCase);
    }
}
