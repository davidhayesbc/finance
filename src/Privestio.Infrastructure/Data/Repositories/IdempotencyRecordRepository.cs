using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly PrivestioDbContext _context;

    public IdempotencyRecordRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyRecord?> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.IdempotencyRecords.FirstOrDefaultAsync(
            r => r.OperationId == operationId,
            cancellationToken
        );

    public async Task<IdempotencyRecord> AddAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default
    )
    {
        await _context.IdempotencyRecords.AddAsync(record, cancellationToken);
        return record;
    }
}
