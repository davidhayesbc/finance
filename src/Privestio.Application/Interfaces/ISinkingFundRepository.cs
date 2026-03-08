using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ISinkingFundRepository
{
    Task<SinkingFund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SinkingFund>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<SinkingFund>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<SinkingFund> AddAsync(
        SinkingFund sinkingFund,
        CancellationToken cancellationToken = default
    );
    Task<SinkingFund> UpdateAsync(
        SinkingFund sinkingFund,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
