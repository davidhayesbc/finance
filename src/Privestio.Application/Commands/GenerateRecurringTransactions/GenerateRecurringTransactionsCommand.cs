using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.GenerateRecurringTransactions;

public record GenerateRecurringTransactionsCommand(Guid UserId, DateTime UpToDate)
    : IRequest<IReadOnlyList<TransactionResponse>>;
