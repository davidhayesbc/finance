using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IAmortizationEntryRepository
{
    Task<IReadOnlyList<AmortizationEntry>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task DeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(
        IEnumerable<AmortizationEntry> entries,
        CancellationToken cancellationToken = default
    );
}
