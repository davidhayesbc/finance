using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Infrastructure.Data.Configurations;

public class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.HouseholdId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.InvitedAt).IsRequired();
        builder.Property(m => m.JoinedAt).IsRequired();

        // Each user can only appear once per household.
        builder
            .HasIndex(m => new { m.HouseholdId, m.UserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Index to quickly find all households a user belongs to.
        builder.HasIndex(m => m.UserId);

        builder
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
