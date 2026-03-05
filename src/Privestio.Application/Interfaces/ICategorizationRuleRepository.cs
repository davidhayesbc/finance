using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ICategorizationRuleRepository
{
    Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorizationRule>> GetEnabledByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<CategorizationRule>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<CategorizationRule> AddAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default
    );
    Task<CategorizationRule> UpdateAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
