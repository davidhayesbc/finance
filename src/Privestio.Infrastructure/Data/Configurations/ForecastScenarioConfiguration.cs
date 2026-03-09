using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class ForecastScenarioConfiguration : IEntityTypeConfiguration<ForecastScenario>
{
    public void Configure(EntityTypeBuilder<ForecastScenario> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);

        builder.Property(f => f.Description).HasMaxLength(1000);

        // Store GrowthAssumptions as JSON
        builder.Property(f => f.GrowthAssumptions).HasColumnType("jsonb");

        builder
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.UserId);

        builder
            .HasIndex(f => new { f.UserId, f.IsDefault })
            .HasFilter("\"IsDefault\" = true AND \"IsDeleted\" = false");
    }
}
