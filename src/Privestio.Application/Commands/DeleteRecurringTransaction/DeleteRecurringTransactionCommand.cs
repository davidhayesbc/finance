using MediatR;

namespace Privestio.Application.Commands.DeleteRecurringTransaction;

public record DeleteRecurringTransactionCommand(Guid RecurringTransactionId, Guid UserId)
    : IRequest;
