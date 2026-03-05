using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    );
    Task<Tag?> FindByNameAsync(
        string name,
        Guid ownerId,
        CancellationToken cancellationToken = default
    );
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<Tag> UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
