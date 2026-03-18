using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateSecurityAlias;

public record UpdateSecurityAliasCommand(
    Guid SecurityId,
    Guid AliasId,
    string Symbol,
    string? Source,
    string? Exchange,
    bool IsPrimary,
    Guid UserId
) : IRequest<SecurityAliasResponse>;
