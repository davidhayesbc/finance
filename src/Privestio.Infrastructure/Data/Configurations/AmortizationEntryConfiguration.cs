using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class AmortizationEntryConfiguration : IEntityTypeConfiguration<AmortizationEntry>
{
    public void Configure(EntityTypeBuilder<AmortizationEntry> builder)
    {
        builder.HasKey(a => a.Id);

        builder.ComplexProperty(
            a => a.PaymentAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("PaymentAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("PaymentCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            a => a.PrincipalAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("PrincipalAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("PrincipalCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            a => a.InterestAmount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("InterestAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("InterestCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            a => a.RemainingBalance,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("RemainingBalanceAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("RemainingBalanceCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(a => a.Account)
            .WithMany()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.AccountId, a.PaymentNumber });
    }
}
