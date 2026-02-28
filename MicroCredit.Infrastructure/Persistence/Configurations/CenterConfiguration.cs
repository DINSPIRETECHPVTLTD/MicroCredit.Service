using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class CenterConfiguration : IEntityTypeConfiguration<Center>
{
    public void Configure(EntityTypeBuilder<Center> builder)
    {
        builder.ToTable("Centers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasOne(x => x.Branch)
            .WithMany(b => b.Centers)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.POCs)
            .WithOne(p => p.Center)
            .HasForeignKey(p => p.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members)
            .WithOne(m => m.Center)
            .HasForeignKey(m => m.CenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
