using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.LoanAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.InterestAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.ProcessingFee)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.InsuranceFee)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.IsSavingEnabled).IsRequired();

        builder.Property(x => x.SavingAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Status).HasMaxLength(20);

        builder.Property(x => x.CollectionTerm)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.NoOfTerms).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.LoanSchedulers)
            .WithOne(ls => ls.Loan)
            .HasForeignKey(ls => ls.LoanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
