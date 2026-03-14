using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetLots;

public record GetLotsQuery(Guid HoldingId, Guid UserId) : IRequest<IReadOnlyList<LotResponse>>;
