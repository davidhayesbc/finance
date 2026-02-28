using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.IdentityUserId)
            .HasMaxLength(450);

        builder.Property(u => u.BaseCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("CAD");

        builder.Property(u => u.Locale)
            .HasMaxLength(20)
            .HasDefaultValue("en-CA");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.IdentityUserId);

        builder.HasOne(u => u.Household)
            .WithMany(h => h.Members)
            .HasForeignKey(u => u.HouseholdId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
