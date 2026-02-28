using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("Investments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.InvestmentDate).IsRequired();
        builder.Property(x => x.CreatedById).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
