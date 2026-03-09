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
    ) => await _context.PriceHistories.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PriceHistory>> GetBySymbolAsync(
        string symbol,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSymbol = symbol.ToUpperInvariant().Trim();
        var query = _context.PriceHistories.Where(p => p.Symbol == normalizedSymbol);

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

    public async Task<bool> ExistsBySymbolAndDateAsync(
        string symbol,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSymbol = symbol.ToUpperInvariant().Trim();
        return await _context.PriceHistories.AnyAsync(
            p => p.Symbol == normalizedSymbol && p.AsOfDate == asOfDate,
            cancellationToken
        );
    }

    public async Task<IReadOnlySet<(string Symbol, DateOnly AsOfDate)>> GetExistingKeysAsync(
        IEnumerable<(string Symbol, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    )
    {
        var keyList = keys.ToList();
        var symbols = keyList.Select(k => k.Symbol).Distinct().ToList();
        var dates = keyList.Select(k => k.AsOfDate).Distinct().ToList();

        var existing = await _context
            .PriceHistories.Where(p => symbols.Contains(p.Symbol) && dates.Contains(p.AsOfDate))
            .Select(p => new { p.Symbol, p.AsOfDate })
            .ToListAsync(cancellationToken);

        return existing.Select(e => (e.Symbol, e.AsOfDate)).ToHashSet();
    }
}
