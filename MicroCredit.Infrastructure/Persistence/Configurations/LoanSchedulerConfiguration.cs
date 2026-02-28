using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class LoanSchedulerConfiguration : IEntityTypeConfiguration<LoanScheduler>
{
    public void Configure(EntityTypeBuilder<LoanScheduler> builder)
    {
        builder.ToTable("LoanSchedulers");

        builder.HasKey(x => x.LoanSchedulerId);

        builder.Property(x => x.LoanId).IsRequired();
        builder.Property(x => x.ScheduleDate).IsRequired();

        builder.Property(x => x.ActualEmiAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualPrincipalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualInterestAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaymentAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SavingAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PrincipalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.InterestAmount).HasColumnType("decimal(18,2)");

        builder.Property(x => x.InstallmentNo).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.PaymentMode).HasMaxLength(50);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();

        builder.HasOne(x => x.Loan)
            .WithMany(l => l.LoanSchedulers)
            .HasForeignKey(x => x.LoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CollectedByUser)
            .WithMany()
            .HasForeignKey(x => x.CollectedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
