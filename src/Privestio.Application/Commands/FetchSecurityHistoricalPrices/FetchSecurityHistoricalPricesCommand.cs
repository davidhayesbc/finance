using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.FetchSecurityHistoricalPrices;

public record FetchSecurityHistoricalPricesCommand(
    Guid SecurityId,
    Guid UserId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<HistoricalPriceSyncResponse>;
