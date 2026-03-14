using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).HasColumnType("numeric(18,8)");
        builder.Property(l => l.Source).HasMaxLength(100);
        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.ComplexProperty(
            l => l.UnitCost,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("UnitCost")
                    .HasColumnType("numeric(18,8)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("UnitCostCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(l => l.Holding)
            .WithMany(h => h.Lots)
            .HasForeignKey(l => l.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.HoldingId, l.AcquiredDate });
    }
}
