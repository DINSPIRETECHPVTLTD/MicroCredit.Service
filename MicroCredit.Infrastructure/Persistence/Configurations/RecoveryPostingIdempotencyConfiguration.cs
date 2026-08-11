using MicroCredit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class RecoveryPostingIdempotencyConfiguration : IEntityTypeConfiguration<RecoveryPostingIdempotency>
{
    public void Configure(EntityTypeBuilder<RecoveryPostingIdempotency> builder)
    {
        builder.ToTable("RecoveryPostingIdempotency");
        builder.HasKey(x => x.ClientRequestId);
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseJson).HasMaxLength(4000);
        builder.Property(x => x.OrgId).IsRequired();
        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired();
        builder.HasIndex(x => new { x.OrgId, x.BranchId, x.CreatedDate });
    }
}
