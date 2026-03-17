using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountValueHistory;

public record GetAccountValueHistoryQuery(
    Guid AccountId,
    Guid UserId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<AccountValueHistoryResponse?>;
