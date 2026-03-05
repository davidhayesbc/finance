using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IImportBatchRepository
{
    Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<ImportBatch> AddAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task<ImportBatch> UpdateAsync(ImportBatch batch, CancellationToken cancellationToken = default);
}
