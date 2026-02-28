using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class POCConfiguration : IEntityTypeConfiguration<POC>
{
    public void Configure(EntityTypeBuilder<POC> builder)
    {
        builder.ToTable("POCs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MiddleName).HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AltPhone).HasMaxLength(20);
        builder.Property(x => x.Address1).HasMaxLength(200);
        builder.Property(x => x.Address2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.ZipCode).HasMaxLength(20);

        builder.Property(x => x.CenterId).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CollectionDay).HasMaxLength(20);
        builder.Property(x => x.CollectionFrequency).IsRequired().HasMaxLength(20);
        builder.Property(x => x.CollectionBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasOne(x => x.Center)
            .WithMany(c => c.POCs)
            .HasForeignKey(x => x.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CollectionByUser)
            .WithMany()
            .HasForeignKey(x => x.CollectionBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members)
            .WithOne(m => m.POC)
            .HasForeignKey(m => m.POCId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
