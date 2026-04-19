using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Configurations;

public class HouseholdInvitationConfiguration : IEntityTypeConfiguration<HouseholdInvitation>
{
    public void Configure(EntityTypeBuilder<HouseholdInvitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.HouseholdId).IsRequired();
        builder.Property(i => i.InvitedEmail).IsRequired().HasMaxLength(254);
        builder.Property(i => i.Token).IsRequired();
        builder
            .Property(i => i.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(i => i.InvitedByUserId).IsRequired();
        builder.Property(i => i.InvitedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
        builder
            .Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Each token is globally unique — used as the invitation link identifier.
        builder.HasIndex(i => i.Token).IsUnique();

        // Find pending invitations by email quickly.
        builder.HasIndex(i => new { i.InvitedEmail, i.Status });

        // Expiry-based cleanup index.
        builder.HasIndex(i => i.ExpiresAt);
    }
}
