using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetChangesSince;

public class GetChangesSinceQueryHandler
    : IRequestHandler<GetChangesSinceQuery, SyncChangesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private const int MaxChangesPerPage = 100;

    public GetChangesSinceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SyncChangesResponse> Handle(
        GetChangesSinceQuery request,
        CancellationToken cancellationToken
    )
    {
        var sinceToken = request.SinceToken ?? DateTime.MinValue;
        var changes = new List<SyncEntityChange>();

        // Get modified accounts since the token
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.UserId,
            cancellationToken
        );

        foreach (var account in accounts)
        {
            if (account.UpdatedAt > sinceToken)
            {
                changes.Add(
                    new SyncEntityChange
                    {
                        EntityType = "Account",
                        EntityId = account.Id,
                        ChangeType = account.CreatedAt > sinceToken ? "Created" : "Updated",
                        ChangedAt = account.UpdatedAt,
                        Payload = JsonSerializer.Serialize(account, JsonSerializerOptions.Default),
                    }
                );
            }
        }

        // Get modified transactions since the token
        var transactions = await _unitOfWork.Transactions.GetByOwnerAndDateRangeAsync(
            request.UserId,
            DateTime.MinValue,
            DateTime.MaxValue,
            cancellationToken
        );

        foreach (var transaction in transactions)
        {
            if (transaction.UpdatedAt > sinceToken)
            {
                changes.Add(
                    new SyncEntityChange
                    {
                        EntityType = "Transaction",
                        EntityId = transaction.Id,
                        ChangeType = transaction.CreatedAt > sinceToken ? "Created" : "Updated",
                        ChangedAt = transaction.UpdatedAt,
                        Payload = JsonSerializer.Serialize(
                            transaction,
                            JsonSerializerOptions.Default
                        ),
                    }
                );
            }
        }

        // Get tombstones for deletions
        var tombstones = await _unitOfWork.SyncTombstones.GetSinceAsync(
            sinceToken,
            cancellationToken
        );

        foreach (var tombstone in tombstones)
        {
            changes.Add(
                new SyncEntityChange
                {
                    EntityType = tombstone.EntityType,
                    EntityId = tombstone.EntityId,
                    ChangeType = "Deleted",
                    ChangedAt = tombstone.DeletedAtUtc,
                }
            );
        }

        // Sort by changed date and apply pagination
        changes = changes.OrderBy(c => c.ChangedAt).ToList();
        var hasMore = changes.Count > MaxChangesPerPage;
        var pagedChanges = changes.Take(MaxChangesPerPage).ToList();

        // Determine new sync token
        var newToken =
            pagedChanges.Count > 0
                ? pagedChanges[^1].ChangedAt
                : request.SinceToken ?? DateTime.UtcNow;

        // Update or create the checkpoint
        var checkpoint = await _unitOfWork.SyncCheckpoints.GetByUserAndDeviceAsync(
            request.UserId,
            request.DeviceId,
            cancellationToken
        );

        if (checkpoint is null)
        {
            checkpoint = new Domain.Entities.SyncCheckpoint(request.UserId, request.DeviceId);
            await _unitOfWork.SyncCheckpoints.AddAsync(checkpoint, cancellationToken);
        }
        else
        {
            checkpoint.UpdateToken(newToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncChangesResponse
        {
            Changes = pagedChanges.AsReadOnly(),
            SyncToken = newToken.ToString("O"),
            HasMore = hasMore,
        };
    }
}
