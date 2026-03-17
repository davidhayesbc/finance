using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SecurityAliasConfiguration : IEntityTypeConfiguration<SecurityAlias>
{
    public void Configure(EntityTypeBuilder<SecurityAlias> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Symbol).IsRequired().HasMaxLength(40);
        builder.Property(a => a.Source).HasMaxLength(100);

        builder
            .HasIndex(a => new
            {
                a.SecurityId,
                a.Symbol,
                a.Source,
            })
            .IsUnique();
        builder.HasIndex(a => a.Symbol);
    }
}
