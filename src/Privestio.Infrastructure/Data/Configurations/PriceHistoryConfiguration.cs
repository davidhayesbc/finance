using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.ComplexProperty(p => p.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasColumnType("numeric(18,6)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3);
        });

        builder.HasIndex(p => new { p.Symbol, p.AsOfDate })
            .IsUnique();

        builder.HasIndex(p => p.AsOfDate);
    }
}
