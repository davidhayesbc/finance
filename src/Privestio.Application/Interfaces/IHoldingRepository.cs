using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IHoldingRepository
{
    Task<Holding?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Holding>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task<Holding?> GetByAccountIdAndSymbolAsync(
        Guid accountId,
        string symbol,
        CancellationToken cancellationToken = default
    );
    Task<Holding> AddAsync(Holding holding, CancellationToken cancellationToken = default);
    Task<Holding> UpdateAsync(Holding holding, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
