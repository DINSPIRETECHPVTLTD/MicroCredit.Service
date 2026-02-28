using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class MasterLookupConfiguration : IEntityTypeConfiguration<MasterLookup>
{
    public void Configure(EntityTypeBuilder<MasterLookup> builder)
    {
        builder.ToTable("MasterLookups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LookupKey).IsRequired();
        builder.Property(x => x.LookupCode).IsRequired();
        builder.Property(x => x.LookupValue).IsRequired();
        builder.Property(x => x.NumericValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.LookupKey, x.LookupCode })
            .IsUnique();
    }
}
