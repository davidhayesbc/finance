using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class HoldingSnapshotRepository : IHoldingSnapshotRepository
{
    private readonly PrivestioDbContext _context;

    public HoldingSnapshotRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HoldingSnapshot>> GetByAccountIdAsync(
        Guid accountId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.HoldingSnapshots.Where(s => s.AccountId == accountId);

        if (fromDate.HasValue)
            query = query.Where(s => s.AsOfDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.AsOfDate <= toDate.Value);

        return await query
            .OrderBy(s => s.AsOfDate)
            .ThenBy(s => s.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<(Guid SecurityId, DateOnly AsOfDate)>> GetExistingKeysAsync(
        Guid accountId,
        IEnumerable<(Guid SecurityId, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    )
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0)
            return new HashSet<(Guid, DateOnly)>();

        var securityIds = keyList.Select(k => k.SecurityId).Distinct().ToList();
        var dates = keyList.Select(k => k.AsOfDate).Distinct().ToList();

        var existing = await _context
            .HoldingSnapshots.Where(s =>
                s.AccountId == accountId
                && securityIds.Contains(s.SecurityId)
                && dates.Contains(s.AsOfDate)
            )
            .Select(s => new { s.SecurityId, s.AsOfDate })
            .ToListAsync(cancellationToken);

        return existing.Select(e => (e.SecurityId, e.AsOfDate)).ToHashSet();
    }

    public async Task AddRangeAsync(
        IEnumerable<HoldingSnapshot> snapshots,
        CancellationToken cancellationToken = default
    )
    {
        await _context.HoldingSnapshots.AddRangeAsync(snapshots, cancellationToken);
    }

    public async Task DeleteByAccountIdAndDateAsync(
        Guid accountId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _context
            .HoldingSnapshots.Where(s => s.AccountId == accountId && s.AsOfDate == asOfDate)
            .ToListAsync(cancellationToken);

        foreach (var snapshot in existing)
        {
            snapshot.SoftDelete();
        }
    }

    public async Task<decimal?> GetCurrentSnapshotTotalAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        // Find the most recent statement date for this account, then sum all snapshots
        // from that single date. Each PDF statement captures the complete portfolio at a
        // point in time, so the latest statement is the authoritative current value.
        // Using per-security-latest would incorrectly include stale values for securities
        // that were sold or rebalanced out between statement dates.
        var latestDate = await _context
            .HoldingSnapshots.Where(s => s.AccountId == accountId)
            .Select(s => (DateOnly?)s.AsOfDate)
            .MaxAsync(cancellationToken);

        if (!latestDate.HasValue)
            return null;

        var total = await _context
            .HoldingSnapshots.Where(s =>
                s.AccountId == accountId && s.AsOfDate == latestDate.Value
            )
            .SumAsync(s => s.MarketValue.Amount, cancellationToken);

        return total;
    }
}
