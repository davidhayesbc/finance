using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class TransactionSplitConfiguration : IEntityTypeConfiguration<TransactionSplit>
{
    public void Configure(EntityTypeBuilder<TransactionSplit> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.ComplexProperty(s => s.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasColumnType("numeric(18,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.TransactionId);
        builder.HasIndex(s => s.CategoryId);
    }
}
