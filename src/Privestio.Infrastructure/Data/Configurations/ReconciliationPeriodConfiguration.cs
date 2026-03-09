using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ReconciliationPeriodConfiguration : IEntityTypeConfiguration<ReconciliationPeriod>
{
    public void Configure(EntityTypeBuilder<ReconciliationPeriod> builder)
    {
        builder.HasKey(r => r.Id);

        builder.ComplexProperty(
            r => r.StatementBalance,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("StatementBalanceAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("StatementBalanceCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(r => r.UnlockReason).HasMaxLength(500);

        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder
            .HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.AccountId, r.StatementDate });

        builder.HasIndex(r => new { r.AccountId, r.Status }).HasFilter("\"IsDeleted\" = false");
    }
}
