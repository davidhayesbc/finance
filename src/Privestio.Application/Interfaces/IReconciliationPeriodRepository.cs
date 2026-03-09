using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IReconciliationPeriodRepository
{
    Task<ReconciliationPeriod?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<ReconciliationPeriod>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
    Task<ReconciliationPeriod> AddAsync(
        ReconciliationPeriod period,
        CancellationToken cancellationToken = default
    );
    Task<ReconciliationPeriod> UpdateAsync(
        ReconciliationPeriod period,
        CancellationToken cancellationToken = default
    );
}
