using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

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
        builder.Property(x => x.Aadhaar).HasMaxLength(20);
        builder.Property(x => x.Occupation).HasMaxLength(100);
        builder.Property(x => x.Relationship).HasMaxLength(100);

        builder.Property(x => x.Age).IsRequired();
        builder.Property(x => x.GuardianFirstName).IsRequired();
        builder.Property(x => x.GuardianMiddleName).HasMaxLength(100);
        builder.Property(x => x.GuardianLastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.GuardianPhone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.GuardianAge).IsRequired();

        builder.Property(x => x.CenterId).IsRequired();
        builder.Property(x => x.POCId).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();

        builder.HasOne(x => x.Center)
            .WithMany(c => c.Members)
            .HasForeignKey(x => x.CenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.POC)
            .WithMany(p => p.Members)
            .HasForeignKey(x => x.POCId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.MemberMembershipFees)
            .WithOne(m => m.Member)
            .HasForeignKey(m => m.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
