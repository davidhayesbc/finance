using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SinkingFundConfiguration : IEntityTypeConfiguration<SinkingFund>
{
    public void Configure(EntityTypeBuilder<SinkingFund> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);

        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.ComplexProperty(
            s => s.TargetAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("TargetAmount")
                    .HasColumnType("numeric(18,4)");
                money.Property(m => m.CurrencyCode).HasColumnName("TargetCurrency").HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            s => s.AccumulatedAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("AccumulatedAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("AccumulatedCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(s => s.Account)
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.UserId, s.IsActive });
    }
}
