using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetRecurringTransactions;

public record GetRecurringTransactionsQuery(Guid UserId, bool ActiveOnly = false)
    : IRequest<IReadOnlyList<RecurringTransactionResponse>>;
