using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateRecurringTransaction;

public record CreateRecurringTransactionCommand(
    Guid UserId,
    Guid AccountId,
    string Description,
    decimal Amount,
    string TransactionType,
    string Frequency,
    DateTime StartDate,
    DateTime? EndDate = null,
    string Currency = "CAD",
    Guid? CategoryId = null,
    Guid? PayeeId = null,
    string? Notes = null
) : IRequest<RecurringTransactionResponse>;
