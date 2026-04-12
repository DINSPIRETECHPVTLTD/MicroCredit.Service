using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Persistence.Scaffolded;

public partial class DinspireMcsDevDbContext : DbContext
{
    public DinspireMcsDevDbContext(DbContextOptions<DinspireMcsDevDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branchs { get; set; }

    public virtual DbSet<Center> Centers { get; set; }

    public virtual DbSet<Investment> Investments { get; set; }

    public virtual DbSet<Ledger> Ledgers { get; set; }

    public virtual DbSet<LedgerTransaction> LedgerTransactions { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<LoanScheduler> LoanSchedulers { get; set; }

    public virtual DbSet<MasterLookup> MasterLookups { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberMembershipFee> MemberMembershipFees { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<PaymentTerm> PaymentTerms { get; set; }

    public virtual DbSet<Poc> Pocs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dinspire_sa");

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_Branchs_CreatedBy");

            entity.HasIndex(e => e.ModifiedBy, "IX_Branchs_ModifiedBy");

            entity.HasIndex(e => e.OrgId, "IX_Branchs_OrgId");

            entity.Property(e => e.Address1).HasMaxLength(200);
            entity.Property(e => e.Address2).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BranchCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.BranchModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);

            entity.HasOne(d => d.Org).WithMany(p => p.Branches)
                .HasForeignKey(d => d.OrgId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Center>(entity =>
        {
            entity.HasIndex(e => e.BranchId, "IX_Centers_BranchId");

            entity.HasIndex(e => e.CreatedBy, "IX_Centers_CreatedBy");

            entity.HasIndex(e => e.ModifiedBy, "IX_Centers_ModifiedBy");

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Branch).WithMany(p => p.Centers)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CenterCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.CenterModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);
        });

        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasIndex(e => e.CreatedById, "IX_Investments_CreatedById");

            entity.HasIndex(e => e.UserId, "IX_Investments_UserId");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.InvestmentCreatedBies)
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.InvestmentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Ledger>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Ledgers_UserId");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.User).WithMany(p => p.Ledgers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<LedgerTransaction>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_LedgerTransactions_CreatedBy");

            entity.HasIndex(e => e.PaidFromUserId, "IX_LedgerTransactions_PaidFromUserId");

            entity.HasIndex(e => e.PaidToUserId, "IX_LedgerTransactions_PaidToUserId");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.TransactionType).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LedgerTransactionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PaidFromUser).WithMany(p => p.LedgerTransactionPaidFromUsers).HasForeignKey(d => d.PaidFromUserId);

            entity.HasOne(d => d.PaidToUser).WithMany(p => p.LedgerTransactionPaidToUsers).HasForeignKey(d => d.PaidToUserId);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_Loans_CreatedBy");

            entity.HasIndex(e => e.MemberId, "IX_Loans_MemberId");

            entity.HasIndex(e => e.ModifiedBy, "IX_Loans_ModifiedBy");

            entity.Property(e => e.CollectionTerm).HasMaxLength(50);
            entity.Property(e => e.InsuranceFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InterestAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoanAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProcessingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SavingAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LoanCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Member).WithMany(p => p.Loans)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.LoanModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);
        });

        modelBuilder.Entity<LoanScheduler>(entity =>
        {
            entity.HasIndex(e => e.CollectedBy, "IX_LoanSchedulers_CollectedBy");

            entity.HasIndex(e => e.CreatedBy, "IX_LoanSchedulers_CreatedBy");

            entity.HasIndex(e => e.LoanId, "IX_LoanSchedulers_LoanId");

            entity.Property(e => e.ActualEmiAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ActualInterestAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ActualPrincipalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.InterestAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentMode).HasMaxLength(50);
            entity.Property(e => e.PrincipalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SavingAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.CollectedByNavigation).WithMany(p => p.LoanSchedulerCollectedByNavigations).HasForeignKey(d => d.CollectedBy);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LoanSchedulerCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Loan).WithMany(p => p.LoanSchedulers)
                .HasForeignKey(d => d.LoanId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MasterLookup>(entity =>
        {
            entity.HasIndex(e => new { e.LookupKey, e.LookupCode }, "IX_MasterLookups_LookupKey_LookupCode").IsUnique();

            entity.Property(e => e.LookupCode).HasMaxLength(50);
            entity.Property(e => e.LookupKey).HasMaxLength(50);
            entity.Property(e => e.LookupValue).HasMaxLength(100);
            entity.Property(e => e.NumericValue).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(e => e.CenterId, "IX_Members_CenterId");

            entity.HasIndex(e => e.CreatedBy, "IX_Members_CreatedBy");

            entity.HasIndex(e => e.ModifiedBy, "IX_Members_ModifiedBy");

            entity.HasIndex(e => e.Pocid, "IX_Members_POCId");

            entity.Property(e => e.Aadhaar).HasMaxLength(20);
            entity.Property(e => e.Address1).HasMaxLength(200);
            entity.Property(e => e.Address2).HasMaxLength(200);
            entity.Property(e => e.AltPhone).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.GuardianDob).HasColumnName("GuardianDOB");
            entity.Property(e => e.GuardianLastName).HasMaxLength(100);
            entity.Property(e => e.GuardianMiddleName).HasMaxLength(100);
            entity.Property(e => e.GuardianPhone).HasMaxLength(20);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.Occupation).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Pocid).HasColumnName("POCId");
            entity.Property(e => e.Relationship).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.Center).WithMany(p => p.Members)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MemberCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.MemberModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);

            entity.HasOne(d => d.Poc).WithMany(p => p.Members)
                .HasForeignKey(d => d.Pocid)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<MemberMembershipFee>(entity =>
        {
            entity.HasIndex(e => e.CollectedBy, "IX_MemberMembershipFees_CollectedBy");

            entity.HasIndex(e => e.CreatedBy, "IX_MemberMembershipFees_CreatedBy");

            entity.HasIndex(e => e.MemberId, "IX_MemberMembershipFees_MemberId");

            entity.HasIndex(e => e.ModifiedBy, "IX_MemberMembershipFees_ModifiedBy");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Comments).HasMaxLength(500);
            entity.Property(e => e.PaymentMode).HasMaxLength(50);

            entity.HasOne(d => d.CollectedByNavigation).WithMany(p => p.MemberMembershipFeeCollectedByNavigations).HasForeignKey(d => d.CollectedBy);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MemberMembershipFeeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Member).WithMany(p => p.MemberMembershipFees)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.MemberMembershipFeeModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.CreatedBy, "IX_Organizations_CreatedBy");

            entity.HasIndex(e => e.ModifiedBy, "IX_Organizations_ModifiedBy");

            entity.Property(e => e.Address1).HasMaxLength(200);
            entity.Property(e => e.Address2).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.OrganizationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.OrganizationModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);
        });

        modelBuilder.Entity<PaymentTerm>(entity =>
        {
            entity.Property(e => e.PaymentTermId).HasColumnName("PaymentTermID");
            entity.Property(e => e.InsuranceFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentTerm1)
                .HasMaxLength(100)
                .HasColumnName("PaymentTerm");
            entity.Property(e => e.PaymentType).HasMaxLength(50);
            entity.Property(e => e.ProcessingFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RateOfInterest).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Poc>(entity =>
        {
            entity.ToTable("POCs");

            entity.HasIndex(e => e.CenterId, "IX_POCs_CenterId");

            entity.HasIndex(e => e.CollectionBy, "IX_POCs_CollectionBy");

            entity.HasIndex(e => e.CreatedBy, "IX_POCs_CreatedBy");

            entity.HasIndex(e => e.ModifiedBy, "IX_POCs_ModifiedBy");

            entity.Property(e => e.Address1).HasMaxLength(200);
            entity.Property(e => e.Address2).HasMaxLength(200);
            entity.Property(e => e.AltPhone).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CollectionDay).HasMaxLength(20);
            entity.Property(e => e.CollectionFrequency).HasMaxLength(20);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.Center).WithMany(p => p.Pocs)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CollectionByNavigation).WithMany(p => p.PocCollectionByNavigations).HasForeignKey(d => d.CollectionBy);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PocCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.PocModifiedByNavigations).HasForeignKey(d => d.ModifiedBy);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.BranchId, "IX_Users_BranchId");

            entity.HasIndex(e => e.CreatedBy, "IX_Users_CreatedBy");

            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.ModifiedBy, "IX_Users_ModifiedBy");

            entity.HasIndex(e => e.OrgId, "IX_Users_OrgId");

            entity.Property(e => e.Address1).HasMaxLength(200);
            entity.Property(e => e.Address2).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.Branch).WithMany(p => p.Users).HasForeignKey(d => d.BranchId);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ModifiedByNavigation).WithMany(p => p.InverseModifiedByNavigation).HasForeignKey(d => d.ModifiedBy);

            entity.HasOne(d => d.Org).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrgId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
