using Privestio.Domain.Entities;

namespace Privestio.Application.Services;

/// <summary>
/// Centralized service for verifying resource-level ownership permissions.
/// Throws <see cref="UnauthorizedAccessException"/> when the caller does not own the resource.
/// </summary>
public class ResourcePermissionService
{
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
}
