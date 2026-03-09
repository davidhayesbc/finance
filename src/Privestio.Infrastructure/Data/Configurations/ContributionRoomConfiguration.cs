using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ContributionRoomConfiguration : IEntityTypeConfiguration<ContributionRoom>
{
    public void Configure(EntityTypeBuilder<ContributionRoom> builder)
    {
        builder.HasKey(c => c.Id);

        builder.ComplexProperty(
            c => c.AnnualLimit,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("AnnualLimitAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("AnnualLimitCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            c => c.CarryForwardRoom,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("CarryForwardAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("CarryForwardCurrency")
                    .HasMaxLength(3);
            }
        );

        builder.ComplexProperty(
            c => c.ContributionsYtd,
            money =>
            {
                money
                    .Property(m => m.Amount)
                    .HasColumnName("ContributionsYtdAmount")
                    .HasColumnType("numeric(18,4)");
                money
                    .Property(m => m.CurrencyCode)
                    .HasColumnName("ContributionsYtdCurrency")
                    .HasMaxLength(3);
            }
        );

        builder
            .HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique: one contribution room per account per year
        builder
            .HasIndex(c => new { c.AccountId, c.Year })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
