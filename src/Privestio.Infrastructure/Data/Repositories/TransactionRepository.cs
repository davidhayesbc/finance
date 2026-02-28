using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Infrastructure.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly PrivestioDbContext _context;

    public TransactionRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Transactions
            .Include(t => t.Splits)
            .Include(t => t.Tags)
            .Include(t => t.Category)
            .Include(t => t.Payee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetPagedAsync(
        Guid accountId,
        int pageSize,
        string? cursor,
        DateRange? dateFilter = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Where(t => t.AccountId == accountId)
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

        // Cursor-based pagination using cursor as "last seen id|date"
        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (parts.Length == 2
                && DateTime.TryParse(parts[0], out var cursorDate)
                && Guid.TryParse(parts[1], out var cursorId))
            {
                query = query.Where(t =>
                    t.Date < cursorDate
                    || (t.Date == cursorDate && t.Id.CompareTo(cursorId) < 0));
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

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
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

    public async Task<bool> FingerprintExistsAsync(string fingerprint, CancellationToken cancellationToken = default)
        => await _context.Transactions
            .AnyAsync(t => t.ImportFingerprint == fingerprint, cancellationToken);
}
