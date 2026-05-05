using MicroCredit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class InsuranceClaimFinancialSummaryConfiguration : IEntityTypeConfiguration<InsuranceClaimFinancialSummary>
{
    public void Configure(EntityTypeBuilder<InsuranceClaimFinancialSummary> builder)
    {
        builder.ToTable("Insurance_Claim_Financial_Summary", "dinspire_sa");

        builder.HasKey(x => x.SummaryId);

        builder.Property(x => x.TotalInsuranceAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.TotalClaimedAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.TotalProcessingFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.TotalJoiningFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.TotalExpenseAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("GETDATE()");
    }
}
