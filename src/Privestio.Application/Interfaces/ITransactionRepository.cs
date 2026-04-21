using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Transaction> Items, string? NextCursor)> GetPagedAsync(
        Guid accountId,
        int pageSize,
        string? cursor,
        DateRange? dateFilter = null,
        Guid? categoryId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default
    );
    Task<Transaction> AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default
    );
    Task AddRangeAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default
    );
    Task<Transaction> UpdateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> FingerprintExistsAsync(
        string fingerprint,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(
        IEnumerable<string> fingerprints,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<Transaction>> GetByImportBatchIdAsync(
        Guid importBatchId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<Transaction>> SearchAsync(
        string searchTerm,
        Guid ownerId,
        int maxResults = 50,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all transactions (with splits) for a user's accounts in a date range.
    /// Used for split-aware budget tracking.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetByOwnerAndDateRangeAsync(
        Guid ownerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns all transactions for the specified account IDs in the given date range.
    /// Use in batch history scenarios to replace per-account <see cref="GetByOwnerAndDateRangeAsync"/>
    /// calls and avoid N×full-table-scan queries.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetByAccountIdsAndDateRangeAsync(
        IEnumerable<Guid> accountIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the signed sum of all account transactions up to and including the
    /// provided transaction position (date/id).
    /// Credits and Transfers contribute +amount; Debits contribute -amount.
    /// </summary>
    Task<decimal> GetSignedSumUpToAsync(
        Guid accountId,
        DateTime upToDate,
        Guid upToId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the signed sum of all transactions for a single account.
    /// Credits and Transfers contribute +amount; Debits contribute -amount.
    /// </summary>
    Task<decimal> GetSignedSumByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the signed sum of all transactions for each of the given accounts.
    /// Missing keys indicate the account has no transactions (sum = 0).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetSignedSumsByAccountIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns uncategorized transaction counts by account ID for the provided account IDs.
    /// Split transactions are excluded because split line categories carry categorization state.
    /// Missing keys indicate an account has zero uncategorized transactions.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetUncategorizedCountsByAccountIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns up to <paramref name="maxRows"/> uncategorized transactions for a single account,
    /// ordered by date descending. Used to feed AI rule suggestion without requiring a file upload.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetUncategorizedByAccountIdAsync(
        Guid accountId,
        int maxRows,
        CancellationToken cancellationToken = default
    );
}
