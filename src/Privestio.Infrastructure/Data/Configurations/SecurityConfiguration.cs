using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SecurityConfiguration : IEntityTypeConfiguration<Security>
{
    public void Configure(EntityTypeBuilder<Security> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CanonicalSymbol).IsRequired().HasMaxLength(40);
        builder.Property(s => s.DisplaySymbol).IsRequired().HasMaxLength(40);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(3);
        builder.Property(s => s.Exchange).HasMaxLength(20);

        builder
            .HasMany(s => s.Aliases)
            .WithOne(a => a.Security)
            .HasForeignKey(a => a.SecurityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(s => s.Identifiers)
            .WithOne(i => i.Security)
            .HasForeignKey(i => i.SecurityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CanonicalSymbol).IsUnique();
        builder.HasIndex(s => s.DisplaySymbol);

        builder.Property(s => s.PricingProviderOrder).HasColumnType("jsonb");
    }
}
