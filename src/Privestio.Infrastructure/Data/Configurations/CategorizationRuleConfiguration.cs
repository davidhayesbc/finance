using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class CategorizationRuleConfiguration : IEntityTypeConfiguration<CategorizationRule>
{
    public void Configure(EntityTypeBuilder<CategorizationRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Conditions).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.Actions).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.Priority });
    }
}
