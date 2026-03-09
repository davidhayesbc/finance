using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class SyncCheckpointConfiguration : IEntityTypeConfiguration<SyncCheckpoint>
{
    public void Configure(EntityTypeBuilder<SyncCheckpoint> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.LastSyncToken).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.DeviceId }).IsUnique();
    }
}
