using MediatR;

namespace Privestio.Application.Commands.DeleteHolding;

public record DeleteHoldingCommand(Guid HoldingId, Guid UserId) : IRequest<bool>;
