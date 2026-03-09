using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ISyncCheckpointRepository
{
    Task<SyncCheckpoint?> GetByUserAndDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default
    );
    Task<SyncCheckpoint> AddAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default
    );
    Task<SyncCheckpoint> UpdateAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default
    );
}
