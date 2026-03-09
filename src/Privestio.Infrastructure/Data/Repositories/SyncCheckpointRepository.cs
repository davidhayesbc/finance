using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class SyncCheckpointRepository : ISyncCheckpointRepository
{
    private readonly PrivestioDbContext _context;

    public SyncCheckpointRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<SyncCheckpoint?> GetByUserAndDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.SyncCheckpoints.FirstOrDefaultAsync(
            c => c.UserId == userId && c.DeviceId == deviceId,
            cancellationToken
        );

    public async Task<SyncCheckpoint> AddAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default
    )
    {
        await _context.SyncCheckpoints.AddAsync(checkpoint, cancellationToken);
        return checkpoint;
    }

    public async Task<SyncCheckpoint> UpdateAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default
    )
    {
        _context.SyncCheckpoints.Update(checkpoint);
        return await Task.FromResult(checkpoint);
    }
}
