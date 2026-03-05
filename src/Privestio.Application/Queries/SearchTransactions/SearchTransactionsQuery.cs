using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.SearchTransactions;

public record SearchTransactionsQuery(string SearchTerm, Guid OwnerId, int MaxResults = 50)
    : IRequest<IReadOnlyList<TransactionResponse>>;
