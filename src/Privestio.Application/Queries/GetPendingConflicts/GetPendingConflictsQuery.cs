using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPendingConflicts;

public record GetPendingConflictsQuery(Guid UserId) : IRequest<IReadOnlyList<SyncConflictResponse>>;
