using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetUserSecurities;

public record GetUserSecuritiesQuery(Guid UserId)
    : IRequest<IReadOnlyList<SecurityCatalogItemResponse>>;
