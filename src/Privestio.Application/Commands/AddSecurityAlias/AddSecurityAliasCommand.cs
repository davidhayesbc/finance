using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AddSecurityAlias;

public record AddSecurityAliasCommand(
    Guid SecurityId,
    string Symbol,
    string Source,
    string? Exchange,
    bool IsPrimary,
    Guid UserId
) : IRequest<SecurityAliasResponse>;
