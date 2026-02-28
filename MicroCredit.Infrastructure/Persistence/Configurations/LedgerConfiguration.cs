using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class LedgerConfiguration : IEntityTypeConfiguration<Ledger>
{
    public void Configure(EntityTypeBuilder<Ledger> builder)
    {
        builder.ToTable("Ledgers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
