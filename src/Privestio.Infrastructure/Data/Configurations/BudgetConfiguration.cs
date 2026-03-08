using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);

        builder.ComplexProperty(
            b => b.Amount,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("numeric(18,4)");
                money.Property(m => m.CurrencyCode).HasColumnName("Currency").HasMaxLength(3);
            }
        );

        builder.Property(b => b.Notes).HasMaxLength(500);

        builder
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique: one budget per user+category+period
        builder
            .HasIndex(b => new
            {
                b.UserId,
                b.CategoryId,
                b.Year,
                b.Month,
            })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(b => new
        {
            b.UserId,
            b.Year,
            b.Month,
        });
    }
}
