using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.RecoveryPosting;
using MicroCredit.Application.Utilities;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
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

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerRecordService _ledgerRecordService;
    private readonly MicroCreditDbContext _db;
    private readonly ILogger<RecoveryPostingService> _logger;

    public RecoveryPostingService(
        IUnitOfWork unitOfWork,
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

        if (request.ClientRequestId == Guid.Empty)
            throw new ArgumentException("ClientRequestId is required.", nameof(request));

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException(
                "At least one recovery line item is required (LoanSchedulerId, amounts, PaymentMode, Status).",
                nameof(request));

        if (request.CollectedBy <= 0)
            throw new ArgumentException("CollectedBy is required (select staff who collected the payment).", nameof(request));

        if (!userContext.BranchId.HasValue)
            throw new InvalidOperationException("Branch context is required to post recovery.");

        var branchId = userContext.BranchId.Value;
        var orgId = userContext.OrgId;

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

            if (payment <= 0)
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentAmount is required and must be greater than zero.");

            if (string.IsNullOrWhiteSpace(line.PaymentMode))
                throw new ArgumentException(
                    $"LoanScheduler {line.LoanSchedulerId}: PaymentMode is required unless status is Overdue.");
        }

        var distinctIds = request.Items.Select(i => i.LoanSchedulerId).Distinct().ToList();
        if (distinctIds.Count != request.Items.Count)
            throw new ArgumentException(
                "Duplicate LoanSchedulerId in the request: each installment row can only be posted once.");

        var snapshots = await _unitOfWork.RecoveryPostings.GetLoanSchedulerSnapshotsForBranchByIdsAsync(
            distinctIds,
            orgId,
            branchId,
            cancellationToken);

        if (snapshots.Count != distinctIds.Count)
            throw new InvalidOperationException(
                "One or more installments were not found, are not base installments, or do not belong to this branch.");

        foreach (var snap in snapshots)
        {
            if (!LoanSchedulerCollectionRules.IsBase(snap.ParentLoanSchedulerId, snap.SubInstallmentSequence))
            {
                throw new InvalidOperationException(
                    $"LoanScheduler {snap.LoanSchedulerId}: recovery posting is only allowed against base installments.");
            }
        }

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

        var requestHash = ComputeRequestHash(request, orgId, branchId);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        RecoveryPostingIdempotency? idempotency = null;
        try
        {
            idempotency = new RecoveryPostingIdempotency(
                request.ClientRequestId,
                orgId,
                branchId,
                userContext.UserId,
                requestHash);

            _db.RecoveryPostingIdempotencies.Add(idempotency);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await tx.RollbackAsync(cancellationToken);
                DetachAddedIdempotency(idempotency);
                return await ReplayIdempotentResponseAsync(
                    request.ClientRequestId,
                    requestHash,
                    orgId,
                    branchId,
                    cancellationToken);
            }

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

                var blocking = await _unitOfWork.RecoveryPostings.CountBlockingEarlierBasesAsync(
                    row.LoanId,
                    row.InstallmentNo,
                    cancellationToken);
                if (blocking > 0)
                {
                    throw new InvalidOperationException(
                        $"LoanScheduler {row.LoanSchedulerId}: earlier installments on this loan still have outstanding or overdue balances and must be posted first.");
                }

                var payment = line.PaymentAmount ?? 0m;

                if (payment <= 0)
                {
                    if (!string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount must be greater than zero.");
                    }
                }

                var dueEmi = row.ActualEmiAmount;
                var dueP = row.ActualPrincipalAmount;
                var dueI = row.ActualInterestAmount;

                if (string.Equals(normalizedStatus, StatusOverdue, StringComparison.OrdinalIgnoreCase))
                {
                    var today = ClientDateTimeConverter.GetCurrentDateInTimeZone(userContext.TimeZoneId);
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
                        nextId = await EnsureNextCarryForwardScheduleAsync(
                            row,
                            dueP,
                            dueI,
                            request.CollectedBy,
                            cancellationToken);
                    }
                    else
                    {
                        await _unitOfWork.RecoveryPostings.AddCarryForwardToScheduleAsync(
                            nextId.Value,
                            dueP,
                            dueI,
                            cancellationToken);
                    }
                }
                else
                {
                    var locked = await _unitOfWork.RecoveryPostings.LockAndGetBaseSchedulerAsync(
                        row.LoanSchedulerId,
                        cancellationToken);

                    if (locked == null)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: installment was not found.");
                    }

                    if (!LoanSchedulerCollectionRules.IsBase(locked)
                        || locked.Status != LoanSchedulerStatus.NotPaid)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: this installment is not a Not Paid base and cannot be posted.");
                    }

                    dueEmi = locked.ActualEmiAmount;
                    dueP = locked.ActualPrincipalAmount;
                    dueI = locked.ActualInterestAmount;

                    if (dueEmi > 0 && RecoveryPaymentSplit.Round2(payment) > RecoveryPaymentSplit.Round2(dueEmi))
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: PaymentAmount cannot exceed the scheduled EMI (ActualEmiAmount).");
                    }

                    if (dueEmi <= 0 && payment > 0)
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: scheduled EMI is zero; a payment cannot be posted.");
                    }

                    var (paidP, paidI) = RecoveryPaymentSplit.CalculatePaymentSplitFromSchedule(
                        dueEmi,
                        dueP,
                        dueI,
                        payment);

                    var isFullPayment =
                        dueEmi > 0
                        && RecoveryPaymentSplit.Round2(payment) >= RecoveryPaymentSplit.Round2(dueEmi);

                    if (!PostedStatusMatchesPayment(normalizedStatus, isFullPayment))
                    {
                        throw new InvalidOperationException(
                            $"LoanScheduler {row.LoanSchedulerId}: Status must be \"Paid\" when the payment covers the full scheduled EMI, or \"Partial Paid\" when it is a partial payment.");
                    }

                    var hasChildren = await _unitOfWork.RecoveryPostings.HasPaymentChildrenAsync(
                        locked.LoanSchedulerId,
                        cancellationToken);

                    if (isFullPayment && !hasChildren)
                    {
                        await _unitOfWork.RecoveryPostings.ApplyFullRecoveryPaymentAsync(
                            locked.LoanSchedulerId,
                            payment,
                            paidP,
                            paidI,
                            request.CollectedBy,
                            line.PaymentMode,
                            line.Comments,
                            cancellationToken);
                    }
                    else if (!isFullPayment)
                    {
                        var remP = RecoveryPaymentSplit.Round2(dueP - paidP);
                        var remI = RecoveryPaymentSplit.Round2(dueI - paidI);
                        var remEmi = RecoveryPaymentSplit.Round2(dueEmi - payment);

                        if (remP < 0 || remI < 0 || remEmi < 0)
                        {
                            throw new InvalidOperationException(
                                $"LoanScheduler {row.LoanSchedulerId}: remaining amounts after partial payment cannot be negative.");
                        }

                        var shrinkRows = await _unitOfWork.RecoveryPostings.TryShrinkBaseForPartialAsync(
                            locked.LoanSchedulerId,
                            dueEmi,
                            dueP,
                            dueI,
                            remEmi,
                            remP,
                            remI,
                            cancellationToken);

                        if (shrinkRows == 0)
                        {
                            throw new InvalidOperationException(
                                $"LoanScheduler {row.LoanSchedulerId}: installment was modified by another request; please reload and try again.");
                        }

                        var seq = await _unitOfWork.RecoveryPostings.GetNextSubInstallmentSequenceAsync(
                            locked.LoanSchedulerId,
                            cancellationToken);

                        var child = LoanScheduler.CreatePaymentChild(
                            locked,
                            seq,
                            payment,
                            paidP,
                            paidI,
                            LoanSchedulerStatus.Partial,
                            request.CollectedBy,
                            line.PaymentMode,
                            line.Comments,
                            userContext.UserId);

                        await _unitOfWork.RecoveryPostings.InsertPaymentChildAsync(child, cancellationToken);
                    }
                    else
                    {
                        // Full payment after prior partial children: Paid child + close base.
                        var seq = await _unitOfWork.RecoveryPostings.GetNextSubInstallmentSequenceAsync(
                            locked.LoanSchedulerId,
                            cancellationToken);

                        var child = LoanScheduler.CreatePaymentChild(
                            locked,
                            seq,
                            payment,
                            paidP,
                            paidI,
                            LoanSchedulerStatus.Paid,
                            request.CollectedBy,
                            line.PaymentMode,
                            line.Comments,
                            userContext.UserId);

                        await _unitOfWork.RecoveryPostings.InsertPaymentChildAsync(child, cancellationToken);

                        var closeRows = await _unitOfWork.RecoveryPostings.TryCloseBaseAfterFinalSettleAsync(
                            locked.LoanSchedulerId,
                            dueEmi,
                            dueP,
                            dueI,
                            cancellationToken);

                        if (closeRows == 0)
                        {
                            throw new InvalidOperationException(
                                $"LoanScheduler {row.LoanSchedulerId}: installment was modified by another request; please reload and try again.");
                        }
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

            var response = new RecoveryPostingPostResponse
            {
                PostedCount = ordered.Count,
                Message = ordered.Count == 1
                    ? "Recovery posting completed successfully for 1 installment."
                    : $"Recovery posting completed successfully for {ordered.Count} installments."
            };

            idempotency.SetResponse(JsonSerializer.Serialize(response));
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Recovery posting post: PostedCount={Count}, BranchId={BranchId}, OrgId={OrgId}, ClientRequestId={ClientRequestId}",
                ordered.Count,
                branchId,
                orgId,
                request.ClientRequestId);

            return response;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RecoveryPostingPostResponse> ReplayIdempotentResponseAsync(
        Guid clientRequestId,
        string requestHash,
        int orgId,
        int branchId,
        CancellationToken cancellationToken)
    {
        RecoveryPostingIdempotency? existing = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            existing = await _db.RecoveryPostingIdempotencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientRequestId == clientRequestId, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException(
                    "A concurrent recovery posting conflicted; please retry with a new ClientRequestId.");
            }

            if (existing.OrgId != orgId || existing.BranchId != branchId)
            {
                throw new InvalidOperationException(
                    "ClientRequestId belongs to a different organization or branch.");
            }

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ClientRequestId was already used with a different recovery posting payload.");
            }

            if (!string.IsNullOrWhiteSpace(existing.ResponseJson))
            {
                var replayed = JsonSerializer.Deserialize<RecoveryPostingPostResponse>(existing.ResponseJson);
                if (replayed == null)
                {
                    throw new InvalidOperationException(
                        "Stored idempotent recovery response could not be deserialized.");
                }

                return replayed;
            }

            await Task.Delay(75, cancellationToken);
        }

        throw new InvalidOperationException(
            "A recovery posting with this ClientRequestId is still in progress. Please retry shortly.");
    }

    private void DetachAddedIdempotency(RecoveryPostingIdempotency idempotency)
    {
        var entry = _db.Entry(idempotency);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2627 || sql.Number == 2601))
                return true;
        }

        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
               || message.Contains("unique index", StringComparison.OrdinalIgnoreCase)
               || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeRequestHash(RecoveryPostingPostRequest request, int orgId, int branchId)
    {
        var sb = new StringBuilder();
        sb.Append("OrgId=").Append(orgId).Append('|');
        sb.Append("BranchId=").Append(branchId).Append('|');
        sb.Append("CollectedBy=").Append(request.CollectedBy).Append('|');
        sb.Append("SkipLedger=").Append(request.SkipLedgerTransaction ? "1" : "0").Append('|');

        foreach (var item in request.Items.OrderBy(i => i.LoanSchedulerId))
        {
            sb.Append("Id=").Append(item.LoanSchedulerId).Append(';');
            sb.Append("Pay=").Append((item.PaymentAmount ?? 0m).ToString("0.00")).Append(';');
            sb.Append("Mode=").Append(item.PaymentMode ?? string.Empty).Append(';');
            sb.Append("Status=").Append(item.Status ?? string.Empty).Append(';');
            sb.Append("Comments=").Append(item.Comments ?? string.Empty).Append('|');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    /// <summary>
    /// When overdue/shortfall has no next unpaid EMI, create one on the next collection date
    /// with the carried-forward principal/interest amounts.
    /// </summary>
    private async Task<int> EnsureNextCarryForwardScheduleAsync(
        RecoveryPostingSchedulerSnapshot row,
        decimal carryPrincipal,
        decimal carryInterest,
        int createdBy,
        CancellationToken cancellationToken)
    {
        if (carryPrincipal < 0 || carryInterest < 0)
        {
            throw new InvalidOperationException(
                $"LoanScheduler {row.LoanSchedulerId}: carry-forward amounts cannot be negative.");
        }

        if (carryPrincipal == 0 && carryInterest == 0)
        {
            throw new InvalidOperationException(
                $"LoanScheduler {row.LoanSchedulerId}: nothing to carry forward onto a new installment.");
        }

        var collectionTerm = await _unitOfWork.RecoveryPostings.GetLoanCollectionTermAsync(
            row.LoanId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(collectionTerm))
        {
            throw new InvalidOperationException(
                $"Loan {row.LoanId}: CollectionTerm is required to create the next installment after overdue.");
        }

        var nextScheduleDate = CalculateNextPaymentDate(row.ScheduleDate, collectionTerm);
        var newId = await _unitOfWork.RecoveryPostings.CreateNextCarryForwardScheduleAsync(
            row.LoanId,
            nextScheduleDate,
            carryPrincipal,
            carryInterest,
            createdBy,
            cancellationToken);

        _logger.LogInformation(
            "Created next carry-forward installment {NewLoanSchedulerId} for Loan {LoanId} after LoanScheduler {SourceLoanSchedulerId} (InstallmentNo {InstallmentNo}).",
            newId,
            row.LoanId,
            row.LoanSchedulerId,
            row.InstallmentNo);

        return newId;
    }

    private static DateTime CalculateNextPaymentDate(DateTime currentDate, string collectionTerm)
    {
        return collectionTerm.Trim().ToLowerInvariant() switch
        {
            "daily" => currentDate.AddDays(1),
            "weekly" => currentDate.AddDays(7),
            "biweekly" or "bi-weekly" => currentDate.AddDays(14),
            "monthly" => currentDate.AddMonths(1),
            "quarterly" => currentDate.AddMonths(3),
            "half-yearly" or "semi-annual" => currentDate.AddMonths(6),
            "yearly" or "annual" => currentDate.AddYears(1),
            _ => currentDate.AddDays(7), // Default to weekly
        };
    }
}
