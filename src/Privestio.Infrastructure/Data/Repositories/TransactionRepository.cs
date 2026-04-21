using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Infrastructure.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly PrivestioDbContext _context;

    public TransactionRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Include(t => t.Splits)
            .Include(t => t.Tags)
            .Include(t => t.Category)
            .Include(t => t.Payee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetPagedAsync(
        Guid accountId,
        int pageSize,
        string? cursor,
        DateRange? dateFilter = null,
        Guid? categoryId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context
            .Transactions.Where(t => t.AccountId == accountId)
            .Include(t => t.Category)
            .Include(t => t.Payee)
            .AsQueryable();

        if (dateFilter.HasValue)
        {
            var startDate = dateFilter.Value.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endDate = dateFilter.Value.End.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= startDate && t.Date <= endDate);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.Description.ToLower().Contains(normalizedSearchTerm)
                || (t.Notes != null && t.Notes.ToLower().Contains(normalizedSearchTerm))
                || (t.Payee != null && t.Payee.DisplayName.ToLower().Contains(normalizedSearchTerm))
                || (t.Category != null && t.Category.Name.ToLower().Contains(normalizedSearchTerm))
            );
        }

        // Cursor-based pagination using cursor as "date|id" with optional trailing metadata.
        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (
                parts.Length >= 2
                && DateTime.TryParseExact(
                    parts[0],
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var cursorDate
                )
                && Guid.TryParse(parts[1], out var cursorId)
            )
            {
                query = query.Where(t =>
                    t.Date < cursorDate || (t.Date == cursorDate && t.Id.CompareTo(cursorId) < 0)
                );
            }
        }

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            var last = items[pageSize - 1];
            nextCursor = $"{last.Date:O}|{last.Id}";
            items = items.Take(pageSize).ToList();
        }

        return (items.AsReadOnly(), nextCursor);
    }

    public async Task<Transaction> AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default
    )
    {
        _context.Transactions.Update(transaction);
        return await Task.FromResult(transaction);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await GetByIdAsync(id, cancellationToken);
        if (transaction is not null)
        {
            transaction.SoftDelete();
        }
    }

    public async Task<bool> FingerprintExistsAsync(
        string fingerprint,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Transactions.AnyAsync(
            t => t.ImportFingerprint == fingerprint,
            cancellationToken
        );

    public async Task AddRangeAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default
    ) => await _context.Transactions.AddRangeAsync(transactions, cancellationToken);

    public async Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(
        IEnumerable<string> fingerprints,
        CancellationToken cancellationToken = default
    )
    {
        var fingerprintList = fingerprints.ToList();
        var existing = await _context
            .Transactions.Where(t =>
                t.ImportFingerprint != null && fingerprintList.Contains(t.ImportFingerprint)
            )
            .Select(t => t.ImportFingerprint!)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet();
    }

    public async Task<IReadOnlyList<Transaction>> GetByImportBatchIdAsync(
        Guid importBatchId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Where(t => t.ImportBatchId == importBatchId)
            .Include(t => t.Category)
            .Include(t => t.Payee)
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> SearchAsync(
        string searchTerm,
        Guid ownerId,
        int maxResults = 50,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedTerm = searchTerm.Trim().ToLower();
        return await _context
            .Transactions.Include(t => t.Category)
            .Include(t => t.Payee)
            .Include(t => t.Account)
            .Where(t =>
                t.Account != null
                && t.Account.OwnerId == ownerId
                && (
                    t.Description.ToLower().Contains(normalizedTerm)
                    || (t.Notes != null && t.Notes.ToLower().Contains(normalizedTerm))
                    || (t.Payee != null && t.Payee.DisplayName.ToLower().Contains(normalizedTerm))
                )
            )
            .OrderByDescending(t => t.Date)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByOwnerAndDateRangeAsync(
        Guid ownerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Include(t => t.Splits)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t =>
                t.Account != null
                && t.Account.OwnerId == ownerId
                && t.Date >= startDate
                && t.Date <= endDate
            )
            .OrderByDescending(t => t.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdsAndDateRangeAsync(
        IEnumerable<Guid> accountIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        var idList = accountIds.ToList();
        if (idList.Count == 0)
            return [];

        return await _context
            .Transactions.Where(t =>
                idList.Contains(t.AccountId) && t.Date >= startDate && t.Date <= endDate
            )
            .OrderBy(t => t.AccountId)
            .ThenBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetSignedSumUpToAsync(
        Guid accountId,
        DateTime upToDate,
        Guid upToId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Where(t =>
                t.AccountId == accountId
                && (t.Date < upToDate || (t.Date == upToDate && t.Id.CompareTo(upToId) <= 0))
            )
            .SumAsync(
                t => t.Type == TransactionType.Debit ? -t.Amount.Amount : t.Amount.Amount,
                cancellationToken
            );

    public async Task<decimal> GetSignedSumByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Transactions.Where(t => t.AccountId == accountId)
            .SumAsync(
                t => t.Type == TransactionType.Debit ? -t.Amount.Amount : t.Amount.Amount,
                cancellationToken
            );

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetSignedSumsByAccountIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default
    )
    {
        var idList = accountIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, decimal>();

        var sums = await _context
            .Transactions.Where(t => idList.Contains(t.AccountId))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                SignedSum = g.Sum(t =>
                    t.Type == TransactionType.Debit ? -t.Amount.Amount : t.Amount.Amount
                ),
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.SignedSum, cancellationToken);

        return sums;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUncategorizedCountsByAccountIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default
    )
    {
        var idList = accountIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, int>();

        var counts = await _context
            .Transactions.Where(t => idList.Contains(t.AccountId) && t.CategoryId == null && !t.Splits.Any())
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Count = g.Count(),
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Count, cancellationToken);

        return counts;
    }
}
