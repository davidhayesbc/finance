using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ImportBatchRepository : IImportBatchRepository
{
    private readonly PrivestioDbContext _context;

    public ImportBatchRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ImportBatch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.ImportBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ImportBatches.Where(b => b.UserId == userId)
            .OrderByDescending(b => b.ImportDate)
            .ToListAsync(cancellationToken);

    public async Task<ImportBatch> AddAsync(
        ImportBatch batch,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ImportBatches.AddAsync(batch, cancellationToken);
        return batch;
    }

    public Task<ImportBatch> UpdateAsync(
        ImportBatch batch,
        CancellationToken cancellationToken = default
    )
    {
        _context.ImportBatches.Update(batch);
        return Task.FromResult(batch);
    }
}
