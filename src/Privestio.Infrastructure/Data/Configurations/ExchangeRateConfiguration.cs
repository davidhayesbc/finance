using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromCurrency).IsRequired().HasMaxLength(3);

        builder.Property(e => e.ToCurrency).IsRequired().HasMaxLength(3);

        builder.Property(e => e.Rate).HasColumnType("numeric(18,8)");

        builder.Property(e => e.Source).IsRequired().HasMaxLength(100);

        builder
            .HasIndex(e => new
            {
                e.FromCurrency,
                e.ToCurrency,
                e.AsOfDate,
            })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.AsOfDate);
    }
}
