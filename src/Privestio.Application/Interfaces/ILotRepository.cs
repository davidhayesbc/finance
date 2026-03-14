using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ILotRepository
{
    Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lot>> GetByHoldingIdAsync(
        Guid holdingId,
        CancellationToken cancellationToken = default
    );
    Task<Lot> AddAsync(Lot lot, CancellationToken cancellationToken = default);
    Task<Lot> UpdateAsync(Lot lot, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
