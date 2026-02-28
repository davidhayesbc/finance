using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetPagedAsync(
        Guid accountId,
        int pageSize,
        string? cursor,
        DateRange? dateFilter = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction> UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> FingerprintExistsAsync(string fingerprint, CancellationToken cancellationToken = default);
}
