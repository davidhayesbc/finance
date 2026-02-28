using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Notes)
            .HasMaxLength(2000);

        builder.Property(t => t.ExternalId)
            .HasMaxLength(200);

        builder.Property(t => t.ImportFingerprint)
            .HasMaxLength(512);

        builder.ComplexProperty(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasColumnType("numeric(18,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.HasMany(t => t.Splits)
            .WithOne(s => s.Transaction)
            .HasForeignKey(s => s.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Payee)
            .WithMany()
            .HasForeignKey(t => t.PayeeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Fingerprint must be unique for idempotent imports
        builder.HasIndex(t => t.ImportFingerprint)
            .IsUnique()
            .HasFilter("\"ImportFingerprint\" IS NOT NULL");

        builder.HasIndex(t => new { t.AccountId, t.Date });
        builder.HasIndex(t => new { t.AccountId, t.IsDeleted });
        builder.HasIndex(t => t.CategoryId);
    }
}
