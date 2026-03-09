using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OperationId).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ResponseData).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();

        builder.HasIndex(r => r.OperationId).IsUnique();
        builder.HasIndex(r => r.ExpiresAt);
    }
}
