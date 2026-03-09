using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPendingConflicts;

public class GetPendingConflictsQueryHandler
    : IRequestHandler<GetPendingConflictsQuery, IReadOnlyList<SyncConflictResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingConflictsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SyncConflictResponse>> Handle(
        GetPendingConflictsQuery request,
        CancellationToken cancellationToken
    )
    {
        var conflicts = await _unitOfWork.SyncConflicts.GetPendingAsync(cancellationToken);

        return conflicts
            .Select(c => new SyncConflictResponse
            {
                Id = c.Id,
                EntityType = c.EntityType,
                EntityId = c.EntityId,
                LocalData = c.LocalData,
                ServerData = c.ServerData,
                Status = c.Status,
                DetectedAt = c.DetectedAt,
                ResolvedAt = c.ResolvedAt,
                Resolution = c.Resolution,
            })
            .ToList()
            .AsReadOnly();
    }
}
