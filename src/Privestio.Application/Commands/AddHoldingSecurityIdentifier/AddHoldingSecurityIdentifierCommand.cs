using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AddHoldingSecurityIdentifier;

public record AddHoldingSecurityIdentifierCommand(
    Guid HoldingId,
    string IdentifierType,
    string Value,
    bool IsPrimary,
    Guid UserId
) : IRequest<SecurityIdentifierResponse>;
