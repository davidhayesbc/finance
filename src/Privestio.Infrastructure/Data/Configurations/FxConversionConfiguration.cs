using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class FxConversionConfiguration : IEntityTypeConfiguration<FxConversion>
{
    public void Configure(EntityTypeBuilder<FxConversion> builder)
    {
        builder.HasKey(f => f.Id);

        builder.ComplexProperty(
            f => f.OriginalAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("OriginalAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("OriginalCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            f => f.ConvertedAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("ConvertedAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("ConvertedCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.Property(f => f.AppliedRate).HasColumnType("numeric(18,8)");

        builder
            .HasOne(f => f.Transaction)
            .WithMany()
            .HasForeignKey(f => f.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(f => f.ExchangeRate)
            .WithMany()
            .HasForeignKey(f => f.ExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.TransactionId).IsUnique();
    }
}
