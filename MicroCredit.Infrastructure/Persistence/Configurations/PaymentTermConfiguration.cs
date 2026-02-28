using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroCredit.Domain.Entities;

namespace MicroCredit.Infrastructure.Persistence.Configurations;

public class PaymentTermConfiguration : IEntityTypeConfiguration<PaymentTerm>
{
    public void Configure(EntityTypeBuilder<PaymentTerm> builder)
    {
        builder.ToTable("PaymentTerms");

        builder.HasKey(x => x.PaymentTermId);

        builder.Property(x => x.PaymentTermId).HasColumnName("PaymentTermID");
        builder.Property(x => x.PaymentTermName).HasColumnName("PaymentTerm");
        builder.Property(x => x.PaymentType).HasColumnName("PaymentType");
        builder.Property(x => x.NoOfTerms).HasColumnName("NoOfTerms");
        builder.Property(x => x.ProcessingFee).HasColumnName("ProcessingFee").HasColumnType("decimal(18,2)");
        builder.Property(x => x.RateOfInterest).HasColumnName("RateOfInterest").HasColumnType("decimal(18,2)");
        builder.Property(x => x.InsuranceFee).HasColumnName("InsuranceFee").HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(x => x.ModifiedBy).HasColumnName("ModifiedBy");
        builder.Property(x => x.ModifiedAt).HasColumnName("ModifiedAt");
        builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
    }
}
