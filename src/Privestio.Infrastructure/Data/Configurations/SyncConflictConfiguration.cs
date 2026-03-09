using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(c => c.EntityId).IsRequired();
        builder.Property(c => c.LocalData).IsRequired();
        builder.Property(c => c.ServerData).IsRequired();
        builder.Property(c => c.Status).IsRequired().HasMaxLength(20);
        builder.Property(c => c.DetectedAt).IsRequired();
        builder.Property(c => c.Resolution).HasMaxLength(20);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => new { c.EntityType, c.EntityId });
    }
}
