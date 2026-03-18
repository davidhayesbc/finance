using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateSecurityDetails;

public record UpdateSecurityDetailsCommand(
    Guid SecurityId,
    string Name,
    string DisplaySymbol,
    string Currency,
    string? Exchange,
    bool IsCashEquivalent,
    Guid UserId
) : IRequest<SecurityCatalogItemResponse>;
