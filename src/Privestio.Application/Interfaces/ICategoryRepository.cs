using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    );
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasLinkedTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
}
