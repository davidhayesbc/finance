using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.SetPricingProviderOrder;

public record SetPricingProviderOrderCommand(
    Guid SecurityId,
    List<string>? ProviderOrder,
    Guid UserId
) : IRequest<SecurityCatalogItemResponse>;
