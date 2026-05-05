using Microsoft.EntityFrameworkCore;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence;

public class MicroCreditDbContext : DbContext
{
    public MicroCreditDbContext(DbContextOptions<MicroCreditDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Center> Centers => Set<Center>();
    public DbSet<InsuranceClaimFinancialSummary> InsuranceClaimFinancialSummaries =>
        Set<InsuranceClaimFinancialSummary>();

    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<Ledger> Ledgers => Set<Ledger>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanScheduler> LoanSchedulers => Set<LoanScheduler>();
    public DbSet<MasterLookup> MasterLookups => Set<MasterLookup>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberMembershipFee> MemberMembershipFees => Set<MemberMembershipFee>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<POC> POCs => Set<POC>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Align with database objects (e.g. [dinspire_sa].[Ledgers]); login default schema is not always used by EF-generated SQL.
        modelBuilder.HasDefaultSchema("dinspire_sa");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicroCreditDbContext).Assembly);
    }
}
