using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ValuationConfiguration : IEntityTypeConfiguration<Valuation>
{
    public void Configure(EntityTypeBuilder<Valuation> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.ComplexProperty(v => v.EstimatedValue, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("EstimatedValueAmount")
                .HasColumnType("numeric(18,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("EstimatedValueCurrency")
                .HasMaxLength(3);
        });

        builder.HasIndex(v => new { v.AccountId, v.EffectiveDate });
    }
}
