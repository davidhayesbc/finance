using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateAccount;

public record UpdateAccountCommand(
    Guid AccountId,
    Guid UserId,
    string Name,
    string? Institution,
    string? Notes,
    bool IsShared
) : IRequest<AccountResponse>;
