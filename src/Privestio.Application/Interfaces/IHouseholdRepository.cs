using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Interfaces;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the household with members and invitations loaded.</summary>
    Task<Household?> GetByIdWithMembersAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns the household the given user belongs to, if any.</summary>
    Task<Household?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<HouseholdInvitation?> GetInvitationByTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default
    );

    Task<HouseholdMember?> GetMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<Household> AddAsync(Household household, CancellationToken cancellationToken = default);

    Task<Household> UpdateAsync(
        Household household,
        CancellationToken cancellationToken = default
    );

    /// <summary>Checks whether the given user is a member of any household (for uniqueness enforcement).</summary>
    Task<bool> IsUserInAnyHouseholdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns the role of a user in a household, or null if not a member.</summary>
    Task<HouseholdRole?> GetMemberRoleAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
