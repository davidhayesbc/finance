using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class HoldingConfiguration : IEntityTypeConfiguration<Holding>
{
    public void Configure(EntityTypeBuilder<Holding> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(h => h.SecurityName).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Quantity).HasColumnType("numeric(18,8)");
        builder.Property(h => h.Notes).HasMaxLength(2000);

        builder.ComplexProperty(
            h => h.AverageCostPerUnit,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("AverageCostPerUnit")
                    .HasColumnType("numeric(18,8)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("AverageCostCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(h => h.Account)
            .WithMany()
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(h => h.Security)
            .WithMany()
            .HasForeignKey(h => h.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(h => h.Lots)
            .WithOne(l => l.Holding)
            .HasForeignKey(l => l.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(h => new { h.AccountId, h.SecurityId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(h => h.AccountId);
    }
}
