using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.SyncHistoricalPrices;

public record SyncHistoricalPricesCommand(Guid UserId, DateOnly FromDate, DateOnly ToDate)
    : IRequest<HistoricalPriceSyncResponse>;
