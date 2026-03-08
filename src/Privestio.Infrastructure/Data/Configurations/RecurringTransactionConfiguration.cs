using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Description).IsRequired().HasMaxLength(500);

        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.ComplexProperty(
            r => r.Amount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("numeric(18,4)");
                money.Property(m => m.CurrencyCode).HasColumnName("Currency").HasMaxLength(3);
            }
        );

        builder.Property(r => r.TransactionType).HasConversion<string>().HasMaxLength(20);

        builder.Property(r => r.Frequency).HasConversion<string>().HasMaxLength(20);

        builder
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(r => r.Payee)
            .WithMany()
            .HasForeignKey(r => r.PayeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.UserId, r.IsActive });
        builder.HasIndex(r => new { r.NextOccurrence, r.IsActive });
    }
}
