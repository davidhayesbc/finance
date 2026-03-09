using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SyncTombstoneConfiguration : IEntityTypeConfiguration<SyncTombstone>
{
    public void Configure(EntityTypeBuilder<SyncTombstone> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(t => t.EntityId).IsRequired();
        builder.Property(t => t.DeletedAtUtc).IsRequired();

        builder.HasIndex(t => t.EntityId);
        builder.HasIndex(t => t.DeletedAtUtc);
        builder.HasIndex(t => new { t.EntityType, t.EntityId });
    }
}
