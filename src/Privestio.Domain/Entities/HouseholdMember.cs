using Privestio.Domain.Enums;

namespace Privestio.Domain.Entities;

/// <summary>
/// Join entity representing a user's membership in a household with a specific role.
/// </summary>
public class HouseholdMember : BaseEntity
{
    private HouseholdMember() { }

    public HouseholdMember(Guid householdId, Guid userId, HouseholdRole role)
    {
        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        InvitedAt = DateTime.UtcNow;
        JoinedAt = DateTime.UtcNow;
    }

    public Guid HouseholdId { get; private set; }
    public Household Household { get; set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;

    public HouseholdRole Role { get; private set; }

    public DateTime InvitedAt { get; private set; }

    /// <summary>When the user accepted the invitation and joined.</summary>
    public DateTime JoinedAt { get; private set; }

    public void UpdateRole(HouseholdRole newRole)
    {
        if (Role == HouseholdRole.Owner)
            throw new InvalidOperationException("The household owner's role cannot be changed.");

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }
}
