using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.RecoveryPosting;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCredit.Application.Tests.Integration;

[Collection("DbIntegration")]
public sealed class RecoveryPostingDbIntegrationTests
{
    private readonly RecoveryPostingIntegrationFixture _fixture;

    public RecoveryPostingDbIntegrationTests(RecoveryPostingIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Test1_Recursive_Partial_500_250_250()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestIds = new List<Guid>();
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 1, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var baseId = seed.BaseSchedulerIds[0];

            var r1 = await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 500m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);

            Assert.Equal(1, r1.PostedCount);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var baserow = await ReloadBaseAsync(db, baseId);
                var children = await LoadChildrenAsync(db, baseId);

                Assert.Equal(500m, baserow.ActualEmiAmount);
                Assert.Equal(LoanSchedulerStatus.NotPaid, baserow.Status);
                Assert.Equal(0m, baserow.PaymentAmount);
                Assert.Single(children);
                Assert.Equal(1, children[0].SubInstallmentSequence);
                Assert.Equal(LoanSchedulerStatus.Partial, children[0].Status);
                Assert.Equal(500m, children[0].PaymentAmount);
                Assert.Equal(baseId, children[0].ParentLoanSchedulerId);
            }

            var r2 = await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 250m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);
            Assert.Equal(1, r2.PostedCount);

            var r3 = await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 250m,
                PaymentMode = "Cash",
                Status = "Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);
            Assert.Equal(1, r3.PostedCount);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var baserow = await ReloadBaseAsync(db, baseId);
                var children = await LoadChildrenAsync(db, baseId);

                Assert.Equal(0m, baserow.ActualEmiAmount);
                Assert.Equal(0m, baserow.ActualPrincipalAmount);
                Assert.Equal(0m, baserow.ActualInterestAmount);
                Assert.Equal(LoanSchedulerStatus.Paid, baserow.Status);
                Assert.Equal(0m, baserow.PaymentAmount);
                Assert.Equal(0m, baserow.PrincipalAmount);
                Assert.Equal(0m, baserow.InterestAmount);

                Assert.Equal(3, children.Count);
                Assert.Equal(LoanSchedulerStatus.Partial, children[0].Status);
                Assert.Equal(LoanSchedulerStatus.Partial, children[1].Status);
                Assert.Equal(LoanSchedulerStatus.Paid, children[2].Status);
                Assert.Equal(1, children[0].SubInstallmentSequence);
                Assert.Equal(2, children[1].SubInstallmentSequence);
                Assert.Equal(3, children[2].SubInstallmentSequence);

                // Reconcile P/I against RecoveryPaymentSplit expectations for 500/250/250.
                decimal dueP = 833.33m, dueI = 166.67m, dueEmi = 1000m;
                var expectedChildren = new List<(decimal Pay, decimal P, decimal I, LoanSchedulerStatus Status)>();
                foreach (var (pay, status) in new[]
                         {
                             (500m, LoanSchedulerStatus.Partial),
                             (250m, LoanSchedulerStatus.Partial),
                             (250m, LoanSchedulerStatus.Paid)
                         })
                {
                    var (paidP, paidI) = RecoveryPaymentSplit.CalculatePaymentSplitFromSchedule(dueEmi, dueP, dueI, pay);
                    expectedChildren.Add((pay, paidP, paidI, status));
                    dueP = RecoveryPaymentSplit.Round2(dueP - paidP);
                    dueI = RecoveryPaymentSplit.Round2(dueI - paidI);
                    dueEmi = RecoveryPaymentSplit.Round2(dueP + dueI);
                }

                Assert.Equal(0m, dueP);
                Assert.Equal(0m, dueI);
                Assert.Equal(0m, dueEmi);

                for (var i = 0; i < 3; i++)
                {
                    Assert.Equal(expectedChildren[i].Pay, children[i].PaymentAmount);
                    Assert.Equal(expectedChildren[i].P, children[i].PrincipalAmount);
                    Assert.Equal(expectedChildren[i].I, children[i].InterestAmount);
                    Assert.Equal(expectedChildren[i].Status, children[i].Status);
                }

                Assert.Equal(
                    833.33m,
                    RecoveryPaymentSplit.Round2(children.Sum(c => c.PrincipalAmount) + baserow.ActualPrincipalAmount));
                Assert.Equal(
                    166.67m,
                    RecoveryPaymentSplit.Round2(children.Sum(c => c.InterestAmount) + baserow.ActualInterestAmount));
                Assert.Equal(1000m, children.Sum(c => c.PaymentAmount));

                var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
                Assert.Equal(3, ledger.Count);
                Assert.Equal(1000m, ledger.Sum(x => x.Amount));
                Assert.All(ledger, x => Assert.Equal("EMI Recovery", x.TransactionType));
            }
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test2_Concurrent_Identical_ClientRequestId()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestId = Guid.NewGuid();
        var clientRequestIds = new List<Guid> { clientRequestId };
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 1, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var baseId = seed.BaseSchedulerIds[0];

            var request = BuildRequest(seed, clientRequestId, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 500m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            });

            using var scope1 = _fixture.CreateScope();
            using var scope2 = _fixture.CreateScope();
            var svc1 = _fixture.GetRecoveryPostingService(scope1);
            var svc2 = _fixture.GetRecoveryPostingService(scope2);

            var results = await Task.WhenAll(
                svc1.PostRecoveriesAsync(CloneRequest(request), _fixture.UserContext),
                svc2.PostRecoveriesAsync(CloneRequest(request), _fixture.UserContext));

            Assert.All(results, r => Assert.Equal(1, r.PostedCount));

            using var verifyScope = _fixture.CreateScope();
            var db = _fixture.GetDb(verifyScope);
            var children = await LoadChildrenAsync(db, baseId);
            Assert.Single(children);
            Assert.Equal(500m, children[0].PaymentAmount);

            var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
            Assert.Single(ledger);
            Assert.Equal(500m, ledger[0].Amount);
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test3_MultiItem_AllValid_Commits()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestIds = new List<Guid>();
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 3, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var clientRequestId = Guid.NewGuid();
            clientRequestIds.Add(clientRequestId);

            var items = seed.BaseSchedulerIds.Select(id => new RecoveryPostingPostLine
            {
                LoanSchedulerId = id,
                PaymentAmount = 1000m,
                PaymentMode = "Cash",
                Status = "Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }).ToList();

            var response = await PostAsync(seed, items, clientRequestId, skipLedger: false);
            Assert.Equal(3, response.PostedCount);

            using var scope = _fixture.CreateScope();
            var db = _fixture.GetDb(scope);

            foreach (var baseId in seed.BaseSchedulerIds)
            {
                var baserow = await ReloadBaseAsync(db, baseId);
                Assert.Equal(LoanSchedulerStatus.Paid, baserow.Status);
                Assert.Equal(1000m, baserow.PaymentAmount);
                Assert.Empty(await LoadChildrenAsync(db, baseId));
            }

            var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
            Assert.Single(ledger);
            Assert.Equal(3000m, ledger[0].Amount);
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test4_MultiItem_MiddleInvalid_FullRollback()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestId = Guid.NewGuid();
        var clientRequestIds = new List<Guid> { clientRequestId };
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 3, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var n = seed.BaseSchedulerIds[0];
            var n2 = seed.BaseSchedulerIds[2];

            var request = BuildRequest(seed, clientRequestId,
                new RecoveryPostingPostLine
                {
                    LoanSchedulerId = n,
                    PaymentAmount = 1000m,
                    PaymentMode = "Cash",
                    Status = "Paid",
                    Comments = RecoveryPostingIntegrationFixture.Marker
                },
                new RecoveryPostingPostLine
                {
                    LoanSchedulerId = n2,
                    PaymentAmount = 1000m,
                    PaymentMode = "Cash",
                    Status = "Paid",
                    Comments = RecoveryPostingIntegrationFixture.Marker
                });

            using (var scope = _fixture.CreateScope())
            {
                var svc = _fixture.GetRecoveryPostingService(scope);
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => svc.PostRecoveriesAsync(request, _fixture.UserContext));
                Assert.Contains("earlier installments", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                foreach (var baseId in seed.BaseSchedulerIds)
                {
                    var baserow = await ReloadBaseAsync(db, baseId);
                    Assert.Equal(LoanSchedulerStatus.NotPaid, baserow.Status);
                    Assert.Equal(1000m, baserow.ActualEmiAmount);
                    Assert.Equal(0m, baserow.PaymentAmount);
                    Assert.Empty(await LoadChildrenAsync(db, baseId));
                }

                var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
                Assert.Empty(ledger);

                var idem = await db.RecoveryPostingIdempotencies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ClientRequestId == clientRequestId);

                Assert.True(
                    idem is null || string.IsNullOrWhiteSpace(idem.ResponseJson),
                    "ClientRequestId should not retain a successful ResponseJson after rollback.");
            }
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test5_ModifyLoan_SkipLedger_UsesSameIdempotency()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestId = Guid.NewGuid();
        var clientRequestIds = new List<Guid> { clientRequestId };
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 1, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var baseId = seed.BaseSchedulerIds[0];

            var request = BuildRequest(seed, clientRequestId, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 500m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            });
            request.SkipLedgerTransaction = true;

            RecoveryPostingPostResponse first;
            using (var scope = _fixture.CreateScope())
            {
                var svc = _fixture.GetRecoveryPostingService(scope);
                first = await svc.PostRecoveriesAsync(request, _fixture.UserContext);
            }

            Assert.Equal(1, first.PostedCount);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var children = await LoadChildrenAsync(db, baseId);
                Assert.Single(children);

                var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
                Assert.Empty(ledger);

                var idem = await db.RecoveryPostingIdempotencies
                    .AsNoTracking()
                    .SingleAsync(x => x.ClientRequestId == clientRequestId);
                Assert.False(string.IsNullOrWhiteSpace(idem.ResponseJson));
            }

            RecoveryPostingPostResponse replay;
            using (var scope = _fixture.CreateScope())
            {
                var svc = _fixture.GetRecoveryPostingService(scope);
                replay = await svc.PostRecoveriesAsync(CloneRequest(request), _fixture.UserContext);
            }

            Assert.Equal(first.PostedCount, replay.PostedCount);
            Assert.Equal(first.Message, replay.Message);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var children = await LoadChildrenAsync(db, baseId);
                Assert.Single(children);
                Assert.Empty(await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart));
            }
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test6_OverdueCarried_Allows_Next_Emi_Payment()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestIds = new List<Guid>();
        SeedContext? seed = null;

        try
        {
            seed = await SeedAsync(baseCount: 2, emi: 1000m, principal: 833.33m, interest: 166.67m);
            var firstId = seed.BaseSchedulerIds[0];
            var secondId = seed.BaseSchedulerIds[1];

            // Overdue requires schedule date before today (UTC test timezone).
            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var past = DateTime.UtcNow.Date.AddDays(-7);
                await db.LoanSchedulers
                    .Where(ls => seed.BaseSchedulerIds.Contains(ls.LoanSchedulerId))
                    .ExecuteUpdateAsync(s => s.SetProperty(ls => ls.ScheduleDate, past));
            }

            var overdueResponse = await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = firstId,
                PaymentAmount = 0m,
                PaymentMode = null,
                Status = "Overdue",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);
            Assert.Equal(1, overdueResponse.PostedCount);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var first = await ReloadBaseAsync(db, firstId);
                var second = await ReloadBaseAsync(db, secondId);

                Assert.Equal(LoanSchedulerStatus.Overdue, first.Status);
                Assert.NotNull(first.PaymentDate);
                Assert.Equal(1000m, first.ActualEmiAmount);
                Assert.Equal(2000m, second.ActualEmiAmount);
                Assert.Equal(LoanSchedulerStatus.NotPaid, second.Status);

                var repo = scope.ServiceProvider
                    .GetRequiredService<MicroCredit.Domain.Interfaces.Repository.IUnitOfWork>()
                    .RecoveryPostings;
                var blocking = await repo.CountBlockingEarlierBasesAsync(
                    seed.LoanId,
                    second.InstallmentNo);
                Assert.Equal(0, blocking);
            }

            var payNext = await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = secondId,
                PaymentAmount = 2000m,
                PaymentMode = "Cash",
                Status = "Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);
            Assert.Equal(1, payNext.PostedCount);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var second = await ReloadBaseAsync(db, secondId);
                Assert.Equal(LoanSchedulerStatus.Paid, second.Status);
                Assert.Equal(2000m, second.PaymentAmount);

                var ledger = await LoadNewEmiRecoveriesAsync(db, seed.LoanId, testStart);
                Assert.Single(ledger);
                Assert.Equal(2000m, ledger[0].Amount);
            }
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    [SkippableFact]
    public async Task Test7_Recursive_Partial_4000_1200_500_2300()
    {
        _fixture.EnsureAvailable();

        var testStart = DateTime.UtcNow;
        var clientRequestIds = new List<Guid>();
        SeedContext? seed = null;

        try
        {
            // 4000 EMI with P/I ratio matching 3333.32 / 666.68 (same shape as Loan 3 EMI 5 face).
            seed = await SeedAsync(baseCount: 1, emi: 4000m, principal: 3333.32m, interest: 666.68m);
            var baseId = seed.BaseSchedulerIds[0];

            await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 1200m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var baserow = await ReloadBaseAsync(db, baseId);
                var children = await LoadChildrenAsync(db, baseId);

                Assert.Equal(2800m, baserow.ActualEmiAmount);
                Assert.Equal(LoanSchedulerStatus.NotPaid, baserow.Status);
                Assert.Equal(0m, baserow.PaymentAmount);
                Assert.Null(baserow.ParentLoanSchedulerId);
                Assert.Equal(0, baserow.SubInstallmentSequence);
                Assert.Single(children);
                Assert.Equal(1, children[0].SubInstallmentSequence);
                Assert.Equal(baseId, children[0].ParentLoanSchedulerId);
                Assert.Equal(LoanSchedulerStatus.Partial, children[0].Status);
                Assert.Equal(1200m, children[0].PaymentAmount);
                Assert.Equal(1200m, children[0].ActualEmiAmount);
                Assert.Equal(baserow.InstallmentNo, children[0].InstallmentNo);
            }

            await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 500m,
                PaymentMode = "Cash",
                Status = "Partial Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var baserow = await ReloadBaseAsync(db, baseId);
                var children = await LoadChildrenAsync(db, baseId);

                Assert.Equal(2300m, baserow.ActualEmiAmount);
                Assert.Equal(LoanSchedulerStatus.NotPaid, baserow.Status);
                Assert.Equal(0m, baserow.PaymentAmount);
                Assert.Equal(2, children.Count);
                Assert.Equal(1200m, children[0].PaymentAmount);
                Assert.Equal(LoanSchedulerStatus.Partial, children[0].Status);
                Assert.Equal(500m, children[1].PaymentAmount);
                Assert.Equal(LoanSchedulerStatus.Partial, children[1].Status);
            }

            await PostAsync(seed, new RecoveryPostingPostLine
            {
                LoanSchedulerId = baseId,
                PaymentAmount = 2300m,
                PaymentMode = "Cash",
                Status = "Paid",
                Comments = RecoveryPostingIntegrationFixture.Marker
            }, clientRequestIds);

            using (var scope = _fixture.CreateScope())
            {
                var db = _fixture.GetDb(scope);
                var baserow = await ReloadBaseAsync(db, baseId);
                var children = await LoadChildrenAsync(db, baseId);

                Assert.Equal(0m, baserow.ActualEmiAmount);
                Assert.Equal(0m, baserow.ActualPrincipalAmount);
                Assert.Equal(0m, baserow.ActualInterestAmount);
                Assert.Equal(LoanSchedulerStatus.Paid, baserow.Status);
                Assert.Equal(0m, baserow.PaymentAmount);
                Assert.Equal(3, children.Count);
                Assert.Equal(LoanSchedulerStatus.Partial, children[0].Status);
                Assert.Equal(LoanSchedulerStatus.Partial, children[1].Status);
                Assert.Equal(LoanSchedulerStatus.Paid, children[2].Status);
                Assert.Equal(1200m, children[0].PaymentAmount);
                Assert.Equal(500m, children[1].PaymentAmount);
                Assert.Equal(2300m, children[2].PaymentAmount);
                Assert.Equal(4000m, children.Sum(c => c.PaymentAmount));

                // No shortfall carried onto any other installment for this loan during the test.
                var otherInflated = await db.LoanSchedulers.AsNoTracking()
                    .Where(ls =>
                        ls.LoanId == seed.LoanId
                        && ls.LoanSchedulerId != baseId
                        && ls.ParentLoanSchedulerId == null
                        && ls.Comments != null
                        && ls.Comments.Contains(RecoveryPostingIntegrationFixture.Marker)
                        && ls.ActualEmiAmount > 4000m)
                    .ToListAsync();
                Assert.Empty(otherInflated);
            }
        }
        finally
        {
            if (seed is not null)
                await CleanupAsync(seed, testStart, clientRequestIds);
        }
    }

    // ---- seed / cleanup / helpers ----

    private async Task<SeedContext> SeedAsync(
        int baseCount,
        decimal emi,
        decimal principal,
        decimal interest)
    {
        using var scope = _fixture.CreateScope();
        var db = _fixture.GetDb(scope);

        // Prefer an existing Active loan with zero Not Paid/Overdue bases (safe append).
        var candidate = await (
            from l in db.Loans.AsNoTracking()
            join m in db.Members.AsNoTracking() on l.MemberId equals m.Id
            join c in db.Centers.AsNoTracking() on m.CenterId equals c.Id
            join b in db.Branches.AsNoTracking() on c.BranchId equals b.Id
            where l.Status == "Active"
                  && !l.IsDeleted
                  && !m.IsDeleted
                  && !c.IsDeleted
                  && !b.IsDeleted
                  && !db.LoanSchedulers.Any(ls =>
                      ls.LoanId == l.Id
                      && ls.ParentLoanSchedulerId == null
                      && ls.SubInstallmentSequence == 0
                      && (ls.Status == LoanSchedulerStatus.NotPaid || ls.Status == LoanSchedulerStatus.Overdue))
            select new
            {
                LoanId = l.Id,
                l.NoOfTerms,
                l.CreatedBy,
                OrgId = b.OrgId,
                BranchId = b.Id
            }).FirstOrDefaultAsync();

        var ownsLoan = false;
        int loanId;
        int orgId;
        int branchId;
        int originalNoOfTerms;
        int createdBy;
        int maxInstallment;

        if (candidate is null)
        {
            // No eligible existing loan in this DB — create a disposable Active loan under an existing member.
            // Cleanup hard-deletes this loan and its schedulers so real business loans are never mutated.
            var host = await (
                from m in db.Members.AsNoTracking()
                join c in db.Centers.AsNoTracking() on m.CenterId equals c.Id
                join b in db.Branches.AsNoTracking() on c.BranchId equals b.Id
                where !m.IsDeleted && !c.IsDeleted && !b.IsDeleted
                select new { MemberId = m.Id, OrgId = b.OrgId, BranchId = b.Id }
            ).FirstOrDefaultAsync();

            Skip.If(
                host is null,
                "No Active loan with zero Not Paid/Overdue bases, and no Member/Center/Branch host available to create a disposable test loan.");

            var provisionalCreatedBy = await db.Users.AsNoTracking()
                .Where(u => !u.IsDeleted && u.OrgId == host!.OrgId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
            Skip.If(provisionalCreatedBy == 0, "No user found to create disposable test loan.");

            var disposableLoan = new Loan(
                memberId: host!.MemberId,
                loanAmount: emi * baseCount,
                interestAmount: interest * baseCount,
                processingFee: 0,
                insuranceFee: 0,
                isSavingEnabled: false,
                savingAmount: 0,
                totalAmount: (emi * baseCount),
                disbursementDate: DateTime.UtcNow.Date,
                collectionTerm: "Monthly",
                collectionStartDate: DateTime.UtcNow.Date,
                noOfTerms: baseCount,
                createdBy: provisionalCreatedBy);

            db.Loans.Add(disposableLoan);
            await db.SaveChangesAsync();

            ownsLoan = true;
            loanId = disposableLoan.Id;
            orgId = host.OrgId;
            branchId = host.BranchId;
            originalNoOfTerms = baseCount;
            createdBy = provisionalCreatedBy;
            maxInstallment = 0;
        }
        else
        {
            loanId = candidate.LoanId;
            orgId = candidate.OrgId;
            branchId = candidate.BranchId;
            originalNoOfTerms = candidate.NoOfTerms;
            createdBy = candidate.CreatedBy > 0 ? candidate.CreatedBy : 0;
            maxInstallment = await db.LoanSchedulers
                .Where(ls => ls.LoanId == loanId)
                .Select(ls => (int?)ls.InstallmentNo)
                .MaxAsync() ?? 0;
        }

        var collectedBy = await db.Users.AsNoTracking()
            .Where(u =>
                !u.IsDeleted
                && u.OrgId == orgId
                && (u.Role == UserRole.Staff || u.Role == UserRole.Owner || u.Role == UserRole.BranchAdmin)
                && db.Ledgers.Any(led => led.UserId == u.Id))
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (collectedBy == 0)
        {
            collectedBy = await db.Users.AsNoTracking()
                .Where(u =>
                    !u.IsDeleted
                    && (u.Role == UserRole.Staff || u.Role == UserRole.Owner || u.Role == UserRole.BranchAdmin))
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        Skip.If(collectedBy == 0, "No Staff/Owner/BranchAdmin user found to use as CollectedBy.");

        if (createdBy <= 0)
            createdBy = collectedBy;

        var baseIds = new List<int>();
        var scheduleDate = DateTime.UtcNow.Date;

        for (var i = 1; i <= baseCount; i++)
        {
            var schedule = new LoanScheduler(
                loanId: loanId,
                scheduleDate: scheduleDate,
                paymentAmount: 0,
                principalAmount: 0,
                interestAmount: 0,
                installmentNo: maxInstallment + i,
                createdBy: createdBy,
                actualEmiAmount: emi,
                actualPrincipalAmount: principal,
                actualInterestAmount: interest,
                savingAmount: 0);

            db.LoanSchedulers.Add(schedule);
            db.Entry(schedule).Property(x => x.Comments).CurrentValue = RecoveryPostingIntegrationFixture.Marker;
            await db.SaveChangesAsync();
            baseIds.Add(schedule.LoanSchedulerId);
        }

        if (!ownsLoan)
        {
            await db.Loans
                .Where(l => l.Id == loanId)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.NoOfTerms, originalNoOfTerms + baseCount));
        }

        _fixture.UserContext.UserId = createdBy;
        _fixture.UserContext.OrgId = orgId;
        _fixture.UserContext.BranchId = branchId;
        _fixture.UserContext.TimeZoneId = "UTC";

        return new SeedContext(
            loanId,
            orgId,
            branchId,
            collectedBy,
            createdBy,
            originalNoOfTerms,
            baseIds,
            ownsLoan);
    }

    private async Task CleanupAsync(SeedContext seed, DateTime testStart, IReadOnlyList<Guid> clientRequestIds)
    {
        try
        {
            using var scope = _fixture.CreateScope();
            var db = _fixture.GetDb(scope);

            var marker = RecoveryPostingIntegrationFixture.Marker;
            var baseIds = seed.BaseSchedulerIds;

            if (seed.OwnsLoan)
            {
                // Disposable loan: remove every scheduler for this loan (children first).
                var children = await db.LoanSchedulers
                    .Where(ls => ls.LoanId == seed.LoanId && ls.ParentLoanSchedulerId != null)
                    .ToListAsync();
                if (children.Count > 0)
                    db.LoanSchedulers.RemoveRange(children);

                var bases = await db.LoanSchedulers
                    .Where(ls => ls.LoanId == seed.LoanId)
                    .ToListAsync();
                if (bases.Count > 0)
                    db.LoanSchedulers.RemoveRange(bases);
            }
            else
            {
                var children = await db.LoanSchedulers
                    .Where(ls =>
                        ls.LoanId == seed.LoanId
                        && ls.ParentLoanSchedulerId != null
                        && baseIds.Contains(ls.ParentLoanSchedulerId.Value))
                    .ToListAsync();
                if (children.Count > 0)
                    db.LoanSchedulers.RemoveRange(children);

                var markedOrSeeded = await db.LoanSchedulers
                    .Where(ls =>
                        ls.LoanId == seed.LoanId
                        && (baseIds.Contains(ls.LoanSchedulerId)
                            || (ls.Comments != null && ls.Comments.Contains(marker))))
                    .ToListAsync();
                if (markedOrSeeded.Count > 0)
                    db.LoanSchedulers.RemoveRange(markedOrSeeded);
            }

            await db.SaveChangesAsync();

            // Delete only ledger rows created for this loan during the test window.
            var newLedger = await db.LedgerTransactions
                .Where(lt =>
                    lt.ReferenceId == seed.LoanId
                    && lt.TransactionType == "EMI Recovery"
                    && lt.CreatedDate >= testStart)
                .ToListAsync();

            foreach (var tx in newLedger)
            {
                if (tx.PaidToUserId is int paidTo)
                {
                    var ledger = await db.Ledgers.FirstOrDefaultAsync(l => l.UserId == paidTo);
                    if (ledger is not null)
                        ledger.UpdateAmount(ledger.Amount - tx.Amount);
                }
            }

            if (newLedger.Count > 0)
                db.LedgerTransactions.RemoveRange(newLedger);

            if (clientRequestIds.Count > 0)
            {
                var idemRows = await db.RecoveryPostingIdempotencies
                    .Where(x => clientRequestIds.Contains(x.ClientRequestId))
                    .ToListAsync();
                if (idemRows.Count > 0)
                    db.RecoveryPostingIdempotencies.RemoveRange(idemRows);
            }

            await db.SaveChangesAsync();

            if (seed.OwnsLoan)
            {
                await db.Loans.Where(l => l.Id == seed.LoanId).ExecuteDeleteAsync();
            }
            else
            {
                await db.Loans
                    .Where(l => l.Id == seed.LoanId)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.NoOfTerms, seed.OriginalNoOfTerms));
            }
        }
        catch (Exception ex)
        {
            // Cleanup must not hide the original assertion failure, but log clearly for operators.
            Console.Error.WriteLine(
                $"PP integration cleanup failed for LoanId={seed.LoanId}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<RecoveryPostingPostResponse> PostAsync(
        SeedContext seed,
        RecoveryPostingPostLine line,
        List<Guid> clientRequestIds)
    {
        var id = Guid.NewGuid();
        clientRequestIds.Add(id);
        return await PostAsync(seed, new[] { line }, id, skipLedger: false);
    }

    private async Task<RecoveryPostingPostResponse> PostAsync(
        SeedContext seed,
        IEnumerable<RecoveryPostingPostLine> lines,
        Guid clientRequestId,
        bool skipLedger)
    {
        var request = BuildRequest(seed, clientRequestId, lines.ToArray());
        request.SkipLedgerTransaction = skipLedger;

        using var scope = _fixture.CreateScope();
        var svc = _fixture.GetRecoveryPostingService(scope);
        return await svc.PostRecoveriesAsync(request, _fixture.UserContext);
    }

    private static RecoveryPostingPostRequest BuildRequest(
        SeedContext seed,
        Guid clientRequestId,
        params RecoveryPostingPostLine[] lines) =>
        new()
        {
            ClientRequestId = clientRequestId,
            CollectedBy = seed.CollectedBy,
            SkipLedgerTransaction = false,
            Items = lines.ToList()
        };

    private static RecoveryPostingPostRequest CloneRequest(RecoveryPostingPostRequest source) =>
        new()
        {
            ClientRequestId = source.ClientRequestId,
            CollectedBy = source.CollectedBy,
            SkipLedgerTransaction = source.SkipLedgerTransaction,
            Items = source.Items.Select(i => new RecoveryPostingPostLine
            {
                LoanSchedulerId = i.LoanSchedulerId,
                PaymentAmount = i.PaymentAmount,
                PrincipalAmount = i.PrincipalAmount,
                InterestAmount = i.InterestAmount,
                PaymentMode = i.PaymentMode,
                Status = i.Status,
                Comments = i.Comments
            }).ToList()
        };

    private static async Task<LoanScheduler> ReloadBaseAsync(MicroCreditDbContext db, int baseId) =>
        await db.LoanSchedulers.AsNoTracking().SingleAsync(ls => ls.LoanSchedulerId == baseId);

    private static async Task<List<LoanScheduler>> LoadChildrenAsync(MicroCreditDbContext db, int baseId) =>
        await db.LoanSchedulers.AsNoTracking()
            .Where(ls => ls.ParentLoanSchedulerId == baseId)
            .OrderBy(ls => ls.SubInstallmentSequence)
            .ToListAsync();

    private static async Task<List<LedgerTransaction>> LoadNewEmiRecoveriesAsync(
        MicroCreditDbContext db,
        int loanId,
        DateTime testStart) =>
        await db.LedgerTransactions.AsNoTracking()
            .Where(lt =>
                lt.ReferenceId == loanId
                && lt.TransactionType == "EMI Recovery"
                && lt.CreatedDate >= testStart)
            .OrderBy(lt => lt.Id)
            .ToListAsync();

    private sealed record SeedContext(
        int LoanId,
        int OrgId,
        int BranchId,
        int CollectedBy,
        int CreatedBy,
        int OriginalNoOfTerms,
        IReadOnlyList<int> BaseSchedulerIds,
        bool OwnsLoan);
}
