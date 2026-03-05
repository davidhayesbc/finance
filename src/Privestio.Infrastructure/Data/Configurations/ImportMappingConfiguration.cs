using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ImportMappingConfiguration : IEntityTypeConfiguration<ImportMapping>
{
    public void Configure(EntityTypeBuilder<ImportMapping> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.FileFormat).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Institution).HasMaxLength(200);
        builder.Property(m => m.DateFormat).HasMaxLength(50);
        builder.Property(m => m.AmountDebitColumn).HasMaxLength(100);
        builder.Property(m => m.AmountCreditColumn).HasMaxLength(100);

        builder.Property(m => m.ColumnMappings).HasColumnType("jsonb");

        builder.HasIndex(m => m.UserId);
    }
}
