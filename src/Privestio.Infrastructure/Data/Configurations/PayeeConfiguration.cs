using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class PayeeConfiguration : IEntityTypeConfiguration<Payee>
{
    public void Configure(EntityTypeBuilder<Payee> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        // Store aliases as a JSON array
        builder.Property<List<string>>("_aliases")
            .HasColumnName("Aliases")
            .HasColumnType("jsonb")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(p => p.DefaultCategory)
            .WithMany()
            .HasForeignKey(p => p.DefaultCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.OwnerId);
    }
}
