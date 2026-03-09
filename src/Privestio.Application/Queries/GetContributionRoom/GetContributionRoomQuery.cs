using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetContributionRoom;

public record GetContributionRoomQuery(Guid AccountId, Guid UserId)
    : IRequest<IReadOnlyList<ContributionRoomResponse>>;
