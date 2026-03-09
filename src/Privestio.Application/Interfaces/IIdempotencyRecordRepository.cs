using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyRecord?> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default
    );
    Task<IdempotencyRecord> AddAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default
    );
}
