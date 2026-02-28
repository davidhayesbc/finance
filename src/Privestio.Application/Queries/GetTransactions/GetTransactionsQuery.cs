using MediatR;
using Privestio.Contracts.Pagination;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetTransactions;

public record GetTransactionsQuery(
    Guid AccountId,
    Guid RequestingUserId,
    int PageSize = 20,
    string? Cursor = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null) : IRequest<PagedResponse<TransactionResponse>>;
