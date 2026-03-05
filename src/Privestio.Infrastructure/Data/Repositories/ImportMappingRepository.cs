using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ImportMappingRepository : IImportMappingRepository
{
    private readonly PrivestioDbContext _context;

    public ImportMappingRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ImportMapping?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Set<ImportMapping>().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImportMapping>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Set<ImportMapping>()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task<ImportMapping> AddAsync(
        ImportMapping mapping,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Set<ImportMapping>().AddAsync(mapping, cancellationToken);
        return mapping;
    }

    public Task<ImportMapping> UpdateAsync(
        ImportMapping mapping,
        CancellationToken cancellationToken = default
    )
    {
        _context.Set<ImportMapping>().Update(mapping);
        return Task.FromResult(mapping);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mapping = await GetByIdAsync(id, cancellationToken);
        if (mapping is not null)
        {
            mapping.SoftDelete();
        }
    }
}
