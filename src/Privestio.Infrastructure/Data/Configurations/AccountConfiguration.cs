using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(a => a.Institution)
            .HasMaxLength(200);

        builder.Property(a => a.AccountNumber)
            .HasMaxLength(100);

        builder.ComplexProperty(a => a.OpeningBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("OpeningBalanceAmount")
                .HasColumnType("numeric(18,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("OpeningBalanceCurrency")
                .HasMaxLength(3);
        });

        builder.ComplexProperty(a => a.CurrentBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentBalanceAmount")
                .HasColumnType("numeric(18,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("CurrentBalanceCurrency")
                .HasMaxLength(3);
        });

        builder.HasIndex(a => a.OwnerId);
        builder.HasIndex(a => new { a.OwnerId, a.IsDeleted });
    }
}
