using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetChangesSince;

public record GetChangesSinceQuery(Guid UserId, string DeviceId, DateTime? SinceToken)
    : IRequest<SyncChangesResponse>;
