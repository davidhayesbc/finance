using MediatR;

namespace Privestio.Application.Commands.DeleteHoldingAlias;

public record DeleteHoldingAliasCommand(Guid HoldingId, Guid AliasId, Guid UserId) : IRequest<bool>;
