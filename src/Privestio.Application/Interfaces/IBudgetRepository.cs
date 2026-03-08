using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<Budget>> GetByUserIdAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    );
    Task<Budget?> GetByUserCategoryPeriodAsync(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    );
    Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<Budget> UpdateAsync(Budget budget, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
