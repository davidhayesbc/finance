using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SecurityIdentifierConfiguration : IEntityTypeConfiguration<SecurityIdentifier>
{
    public void Configure(EntityTypeBuilder<SecurityIdentifier> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IdentifierType).IsRequired();
        builder.Property(i => i.Value).IsRequired().HasMaxLength(32);

        builder.HasIndex(i => new { i.IdentifierType, i.Value }).IsUnique();
        builder.HasIndex(i => new { i.SecurityId, i.IdentifierType, i.Value }).IsUnique();
    }
}
