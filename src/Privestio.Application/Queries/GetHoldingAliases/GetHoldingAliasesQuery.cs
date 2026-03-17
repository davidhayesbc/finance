using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingAliases;

public record GetHoldingAliasesQuery(Guid HoldingId, Guid UserId)
    : IRequest<IReadOnlyList<SecurityAliasResponse>>;
