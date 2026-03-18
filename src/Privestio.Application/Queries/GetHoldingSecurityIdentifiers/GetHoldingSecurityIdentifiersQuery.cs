using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldingSecurityIdentifiers;

public record GetHoldingSecurityIdentifiersQuery(Guid HoldingId, Guid UserId)
    : IRequest<IReadOnlyList<SecurityIdentifierResponse>>;
