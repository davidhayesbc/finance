using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPriceHistory;

public record GetPriceHistoryQuery(
    string Symbol,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<IReadOnlyList<PriceHistoryResponse>>;
