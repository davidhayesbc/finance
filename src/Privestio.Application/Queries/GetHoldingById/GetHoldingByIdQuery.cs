using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingById;

public record GetHoldingByIdQuery(Guid HoldingId, Guid UserId) : IRequest<HoldingResponse?>;
