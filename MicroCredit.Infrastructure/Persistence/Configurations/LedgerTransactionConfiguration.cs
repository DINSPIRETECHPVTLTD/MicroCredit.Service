using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.PaymentDate).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();
        builder.Property(x => x.TransactionType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.FromUser)
            .WithMany()
            .HasForeignKey(x => x.PaidFromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToUser)
            .WithMany()
            .HasForeignKey(x => x.PaidToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
