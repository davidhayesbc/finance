using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly PrivestioDbContext _context;

    public PriceHistoryRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<PriceHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .PriceHistories.Include(p => p.Security)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PriceHistory>> GetBySecurityIdAsync(
        Guid securityId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context
            .PriceHistories.Include(p => p.Security)
            .Where(p => p.SecurityId == securityId);

        if (fromDate.HasValue)
            query = query.Where(p => p.AsOfDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.AsOfDate <= toDate.Value);

        return await query.OrderByDescending(p => p.AsOfDate).ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory> AddAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default
    )
    {
        await _context.PriceHistories.AddAsync(priceHistory, cancellationToken);
        return priceHistory;
    }

    public async Task AddRangeAsync(
        IEnumerable<PriceHistory> priceHistories,
        CancellationToken cancellationToken = default
    )
    {
        await _context.PriceHistories.AddRangeAsync(priceHistories, cancellationToken);
    }

    public async Task<bool> ExistsBySecurityIdAndDateAsync(
        Guid securityId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.PriceHistories.AnyAsync(
            p => p.SecurityId == securityId && p.AsOfDate == asOfDate,
            cancellationToken
        );
    }

    public async Task<IReadOnlySet<(Guid SecurityId, DateOnly AsOfDate)>> GetExistingKeysAsync(
        IEnumerable<(Guid SecurityId, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    )
    {
        var keyList = keys.ToList();
        var securityIds = keyList.Select(k => k.SecurityId).Distinct().ToList();
        var dates = keyList.Select(k => k.AsOfDate).Distinct().ToList();

        var existing = await _context
            .PriceHistories.Where(p =>
                securityIds.Contains(p.SecurityId) && dates.Contains(p.AsOfDate)
            )
            .Select(p => new { p.SecurityId, p.AsOfDate })
            .ToListAsync(cancellationToken);

        return existing.Select(e => (e.SecurityId, e.AsOfDate)).ToHashSet();
    }

    public async Task<IReadOnlyDictionary<Guid, PriceHistory>> GetLatestBySecurityIdsAsync(
        IEnumerable<Guid> securityIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = securityIds.Distinct().ToList();

        var all = await _context
            .PriceHistories.Include(p => p.Security)
            .Where(p => ids.Contains(p.SecurityId))
            .OrderByDescending(p => p.AsOfDate)
            .ToListAsync(cancellationToken);

        return all.GroupBy(p => p.SecurityId).ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>>> GetBySecurityIdsAsync(
        IEnumerable<Guid> securityIds,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var ids = securityIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<PriceHistory>>();

        var query = _context.PriceHistories.Where(p => ids.Contains(p.SecurityId));

        if (toDate.HasValue)
            query = query.Where(p => p.AsOfDate <= toDate.Value);

        var all = await query
            .OrderBy(p => p.SecurityId)
            .ThenBy(p => p.AsOfDate)
            .ToListAsync(cancellationToken);

        return all.GroupBy(p => p.SecurityId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PriceHistory>)g.ToList()
            );
    }

    public async Task DeleteExternalPricesForSecuritiesAsync(
        IEnumerable<Guid> securityIds,
        CancellationToken cancellationToken = default
    )
    {
        var ids = securityIds.ToList();
        await _context
            .PriceHistories.Where(p => ids.Contains(p.SecurityId) && p.Source != "PDFStatement")
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.PriceHistories.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public Task<PriceHistory> UpdateAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default
    )
    {
        _context.PriceHistories.Update(priceHistory);
        return Task.FromResult(priceHistory);
    }
}
