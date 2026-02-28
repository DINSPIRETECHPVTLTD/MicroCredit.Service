using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branchs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Address1).HasMaxLength(200);
        builder.Property(x => x.Address2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.ZipCode).HasMaxLength(20);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);

        builder.Property(x => x.OrgId).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.Branches)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Users)
            .WithOne(u => u.Branch)
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Centers)
            .WithOne(c => c.Branch)
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
