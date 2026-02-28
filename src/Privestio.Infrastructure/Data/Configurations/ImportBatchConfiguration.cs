using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.FileFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.ImportDate);
    }
}
