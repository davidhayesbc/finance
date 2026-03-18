using MediatR;

namespace Privestio.Application.Commands.DeleteHoldingSecurityIdentifier;

public record DeleteHoldingSecurityIdentifierCommand(Guid HoldingId, Guid IdentifierId, Guid UserId)
    : IRequest<bool>;
