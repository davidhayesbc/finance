using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateRecurringTransaction;

public record UpdateRecurringTransactionCommand(
    Guid RecurringTransactionId,
    Guid UserId,
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
