using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MiddleName).HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);

        // DB has Role/Level as nvarchar (e.g. 'Owner', 'Investor'); map to enum
        builder.Property(x => x.Role)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasConversion(new ValueConverter<UserRole, string>(
                v => v.ToString(),
                v => ParseUserRole(v)));

        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.Address1).HasMaxLength(200);
        builder.Property(x => x.Address2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.ZipCode).HasMaxLength(20);

        builder.Property(x => x.OrgId).IsRequired();

        builder.Property(x => x.Level)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasConversion(new ValueConverter<UserLevel, string>(
                v => v.ToString(),
                v => ParseUserLevel(v)));
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany(b => b.Users)
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
    }

    private static UserRole ParseUserRole(string v)
    {
        if (int.TryParse(v, out var n))
            return (UserRole)n;
        return Enum.Parse<UserRole>(v);
    }

    private static UserLevel ParseUserLevel(string v)
    {
        if (int.TryParse(v, out var n))
            return (UserLevel)n;
        return Enum.Parse<UserLevel>(v);
    }
}
