using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.ResolveConflict;

public class ResolveConflictCommandHandler
    : IRequestHandler<ResolveConflictCommand, SyncConflictResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResolveConflictCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SyncConflictResponse> Handle(
        ResolveConflictCommand request,
        CancellationToken cancellationToken
    )
    {
        var conflict =
            await _unitOfWork.SyncConflicts.GetByIdAsync(request.ConflictId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conflict {request.ConflictId} not found.");

        conflict.Resolve(request.Resolution, request.MergedData);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncConflictResponse
        {
            Id = conflict.Id,
            EntityType = conflict.EntityType,
            EntityId = conflict.EntityId,
            LocalData = conflict.LocalData,
            ServerData = conflict.ServerData,
            Status = conflict.Status,
            DetectedAt = conflict.DetectedAt,
            ResolvedAt = conflict.ResolvedAt,
            Resolution = conflict.Resolution,
        };
    }
}
