using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IImportMappingRepository
{
    Task<ImportMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportMapping>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<ImportMapping> AddAsync(
        ImportMapping mapping,
        CancellationToken cancellationToken = default
    );
    Task<ImportMapping> UpdateAsync(
        ImportMapping mapping,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
