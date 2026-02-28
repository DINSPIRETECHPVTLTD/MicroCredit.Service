using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class MemberMembershipFeeConfiguration : IEntityTypeConfiguration<MemberMembershipFee>
{
    public void Configure(EntityTypeBuilder<MemberMembershipFee> builder)
    {
        builder.ToTable("MemberMembershipFees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.PaymentMode).HasMaxLength(50);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.Member)
            .WithMany(m => m.MemberMembershipFees)
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

        builder.HasOne(x => x.CollectedByUser)
            .WithMany()
            .HasForeignKey(x => x.CollectedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
