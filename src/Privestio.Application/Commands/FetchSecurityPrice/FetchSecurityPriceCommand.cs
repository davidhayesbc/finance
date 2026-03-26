using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.FetchSecurityPrice;

public record FetchSecurityPriceCommand(Guid SecurityId, Guid UserId)
    : IRequest<SecurityCatalogItemResponse>;
