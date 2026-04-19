using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Services;

/// <summary>
/// Centralized service for verifying resource-level ownership and household-role permissions.
/// Throws <see cref="UnauthorizedAccessException"/> when the caller does not have access.
/// </summary>
public class ResourcePermissionService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResourcePermissionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public void EnsureAccountOwnership(Account account, Guid userId)
    {
        if (account.OwnerId != userId)
            throw new UnauthorizedAccessException(
                "You do not have permission to access this account."
            );
    }

    public void EnsureBudgetOwnership(Budget budget, Guid userId)
    {
        if (budget.UserId != userId)
            throw new UnauthorizedAccessException(
                "You do not have permission to access this budget."
            );
    }

    public void EnsureScenarioOwnership(ForecastScenario scenario, Guid userId)
    {
        if (scenario.UserId != userId)
            throw new UnauthorizedAccessException(
                "You do not have permission to access this forecast scenario."
            );
    }

    public void EnsureRuleOwnership(CategorizationRule rule, Guid userId)
    {
        if (rule.UserId != userId)
            throw new UnauthorizedAccessException(
                "You do not have permission to access this categorization rule."
            );
    }

    /// <summary>Throws if the user is not a member of the household (any role).</summary>
    public async Task EnsureHouseholdMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _unitOfWork.Households.GetMemberRoleAsync(
            householdId,
            userId,
            cancellationToken
        );

        if (role is null)
            throw new UnauthorizedAccessException(
                "You are not a member of this household."
            );
    }

    /// <summary>
    /// Throws if the user's role is below the required minimum role (Owner > Admin > Member > Viewer).
    /// </summary>
    public async Task EnsureHouseholdRoleAsync(
        Guid householdId,
        Guid userId,
        HouseholdRole minimumRole,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _unitOfWork.Households.GetMemberRoleAsync(
            householdId,
            userId,
            cancellationToken
        );

        if (role is null || role > minimumRole)
            throw new UnauthorizedAccessException(
                $"You need at least the '{minimumRole}' role for this operation."
            );
    }

    /// <summary>Throws if the user is not the owner of the household.</summary>
    public async Task EnsureHouseholdOwnerAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureHouseholdRoleAsync(householdId, userId, HouseholdRole.Owner, cancellationToken);
    }
}
