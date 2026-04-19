using Privestio.Domain.Enums;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a pending invitation for a user to join a household.
/// </summary>
public class HouseholdInvitation : BaseEntity
{
    private HouseholdInvitation() { }

    public HouseholdInvitation(
        Guid householdId,
        string invitedEmail,
        HouseholdRole role,
        Guid invitedByUserId,
        TimeSpan? expiryDuration = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invitedEmail);

        HouseholdId = householdId;
        InvitedEmail = invitedEmail.ToLowerInvariant().Trim();
        Role = role;
        InvitedByUserId = invitedByUserId;
        Token = Guid.NewGuid();
        InvitedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(expiryDuration ?? TimeSpan.FromDays(7));
        Status = HouseholdInvitationStatus.Pending;
    }

    public Guid HouseholdId { get; private set; }
    public Household Household { get; set; } = null!;

    /// <summary>The email address the invitation was sent to.</summary>
    public string InvitedEmail { get; private set; } = string.Empty;

    /// <summary>Cryptographically random token used in the invitation link.</summary>
    public Guid Token { get; private set; }

    public HouseholdRole Role { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public DateTime InvitedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    public HouseholdInvitationStatus Status { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => Status == HouseholdInvitationStatus.Pending && !IsExpired;

    /// <summary>
    /// Marks the invitation as accepted. Validates the claiming email matches the invite.
    /// </summary>
    public void Accept(string claimingEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimingEmail);

        if (!IsValid)
            throw new InvalidOperationException(
                "Invitation is no longer valid (expired or already used)."
            );

        if (
            !string.Equals(
                InvitedEmail,
                claimingEmail.ToLowerInvariant().Trim(),
                StringComparison.Ordinal
            )
        )
            throw new InvalidOperationException(
                "The invitation was issued for a different email address."
            );

        Status = HouseholdInvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (Status != HouseholdInvitationStatus.Pending)
            throw new InvalidOperationException("Only pending invitations can be revoked.");

        Status = HouseholdInvitationStatus.Revoked;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum HouseholdInvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2,
    Expired = 3,
}
