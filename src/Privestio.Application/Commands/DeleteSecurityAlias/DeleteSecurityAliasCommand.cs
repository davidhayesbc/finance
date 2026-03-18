using MediatR;

namespace Privestio.Application.Commands.DeleteSecurityAlias;

public record DeleteSecurityAliasCommand(Guid SecurityId, Guid AliasId, Guid UserId)
    : IRequest<bool>;
