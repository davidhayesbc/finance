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
        // For each security held in this account, find the latest snapshot date and sum
        // that snapshot's MarketValue. This mirrors the logic in HistoricalValueTimelineService
        // which also prefers snapshots as the authoritative source for managed-fund accounts.
        var latestDatePerSecurity = await _context
            .HoldingSnapshots.Where(s => s.AccountId == accountId && !s.IsDeleted)
            .GroupBy(s => s.SecurityId)
            .Select(g => new { SecurityId = g.Key, LatestDate = g.Max(s => s.AsOfDate) })
            .ToListAsync(cancellationToken);

        if (latestDatePerSecurity.Count == 0)
            return null;

        var total = 0m;
        foreach (var entry in latestDatePerSecurity)
        {
            var latestSnapshot = await _context
                .HoldingSnapshots.Where(s =>
                    s.AccountId == accountId
                    && s.SecurityId == entry.SecurityId
                    && s.AsOfDate == entry.LatestDate
                    && !s.IsDeleted
                )
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestSnapshot is not null)
                total += latestSnapshot.MarketValue.Amount;
        }

        return total;
    }
}
