using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateAccount;

public record CreateAccountCommand(
    string Name,
    string AccountType,
    string AccountSubType,
    string Currency,
    decimal OpeningBalance,
    DateTime OpeningDate,
    Guid OwnerId,
    string? Institution = null,
    string? AccountNumber = null,
    string? Notes = null) : IRequest<AccountResponse>;
