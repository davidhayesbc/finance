using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IRecurringTransactionRepository
{
    Task<RecurringTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<RecurringTransaction>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<RecurringTransaction>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<RecurringTransaction>> GetDueBeforeAsync(
        DateTime date,
        CancellationToken cancellationToken = default
    );
    Task<RecurringTransaction> AddAsync(
        RecurringTransaction recurring,
        CancellationToken cancellationToken = default
    );
    Task<RecurringTransaction> UpdateAsync(
        RecurringTransaction recurring,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
