using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IValuationRepository
{
    Task<Valuation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Valuation>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task<Valuation?> GetLatestByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task<Valuation> AddAsync(Valuation valuation, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(
        Guid accountId,
        DateOnly effectiveDate,
        string source,
        CancellationToken cancellationToken = default
    );
}
