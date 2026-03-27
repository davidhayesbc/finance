using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class HoldingSnapshotConfiguration : IEntityTypeConfiguration<HoldingSnapshot>
{
    public void Configure(EntityTypeBuilder<HoldingSnapshot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(s => s.SecurityName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Quantity).HasColumnType("numeric(18,8)");
        builder.Property(s => s.Source).IsRequired().HasMaxLength(100);

        builder.ComplexProperty(
            s => s.UnitPrice,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasColumnType("numeric(18,8)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            s => s.MarketValue,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("MarketValueAmount")
                    .HasColumnType("numeric(18,2)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("MarketValueCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(s => s.Security)
            .WithMany()
            .HasForeignKey(s => s.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(s => new
            {
                s.AccountId,
                s.SecurityId,
                s.AsOfDate,
            })
            .IsUnique();

        builder.HasIndex(s => new { s.AccountId, s.AsOfDate });
    }
}
