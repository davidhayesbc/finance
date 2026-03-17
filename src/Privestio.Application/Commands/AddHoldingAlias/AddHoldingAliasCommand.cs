using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AddHoldingAlias;

public record AddHoldingAliasCommand(
    Guid HoldingId,
    string Symbol,
    string? Source,
    bool IsPrimary,
    Guid UserId
) : IRequest<SecurityAliasResponse>;
