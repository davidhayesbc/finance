using Privestio.Domain.Enums;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a household grouping multiple users for shared finances.
/// </summary>
public class Household : BaseEntity
{
    private readonly List<HouseholdMember> _members = [];
    private readonly List<HouseholdInvitation> _invitations = [];

    private Household() { }

    /// <summary>Creates a new household with the given owner as the first member.</summary>
    public Household(string name, Guid ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        OwnerId = ownerId;

        // Owner is added as the first member automatically.
        _members.Add(new HouseholdMember(Id, ownerId, HouseholdRole.Owner));
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The user who created this household. This role is immutable.</summary>
    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<HouseholdInvitation> Invitations => _invitations.AsReadOnly();

    /// <summary>Adds a member with the given role. Idempotent if already a member.</summary>
    public void AddMember(Guid userId, HouseholdRole role)
    {
        if (role == HouseholdRole.Owner)
            throw new InvalidOperationException(
                "Only one owner is allowed per household. Use Admin role instead."
            );

        if (!_members.Any(m => m.UserId == userId))
        {
            _members.Add(new HouseholdMember(Id, userId, role));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Removes a member from the household. The owner cannot be removed.</summary>
    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
            throw new InvalidOperationException("The household owner cannot be removed.");

        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing is not null)
        {
            _members.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Updates the role of an existing member. The owner's role cannot be changed.</summary>
    public void UpdateMemberRole(Guid userId, HouseholdRole newRole)
    {
        if (userId == OwnerId)
            throw new InvalidOperationException("The household owner's role cannot be changed.");

        if (newRole == HouseholdRole.Owner)
            throw new InvalidOperationException(
                "Cannot assign the Owner role. Ownership transfer is not supported."
            );

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this household.");

        member.UpdateRole(newRole);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Creates a pending invitation for the specified email address.</summary>
    public HouseholdInvitation CreateInvitation(
        string invitedEmail,
        HouseholdRole role,
        Guid invitedByUserId,
        TimeSpan? expiryDuration = null
    )
    {
        if (role == HouseholdRole.Owner)
            throw new InvalidOperationException("Cannot invite a user as Owner.");

        // Revoke any existing pending invitation for the same email.
        var existing = _invitations.FirstOrDefault(i =>
            i.InvitedEmail == invitedEmail.ToLowerInvariant().Trim()
            && i.Status == HouseholdInvitationStatus.Pending
        );
        existing?.Revoke();

        var invitation = new HouseholdInvitation(
            Id,
            invitedEmail,
            role,
            invitedByUserId,
            expiryDuration
        );
        _invitations.Add(invitation);
        UpdatedAt = DateTime.UtcNow;
        return invitation;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public HouseholdRole? GetMemberRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;
}
