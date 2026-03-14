using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetLotById;

public record GetLotByIdQuery(Guid LotId, Guid UserId) : IRequest<LotResponse?>;
