using MediatR;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetNetWorthHistory;

public class GetNetWorthHistoryQueryHandler
    : IRequestHandler<GetNetWorthHistoryQuery, NetWorthHistoryResponse>
{
    private readonly HistoricalValueTimelineService _historicalValueTimelineService;

    public GetNetWorthHistoryQueryHandler(
        HistoricalValueTimelineService historicalValueTimelineService
    )
    {
        _historicalValueTimelineService = historicalValueTimelineService;
    }

    public async Task<NetWorthHistoryResponse> Handle(
        GetNetWorthHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var history = await _historicalValueTimelineService.GetNetWorthHistoryAsync(
            request.UserId,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );

        var accountHistories =
            await _historicalValueTimelineService.GetNetWorthHistoryByAccountAsync(
                request.UserId,
                request.FromDate,
                request.ToDate,
                cancellationToken
            );

        var series = accountHistories
            .GroupBy(ah => new
            {
                ah.AccountId,
                ah.AccountName,
                ah.AccountType,
            })
            .Select(g => new AccountNetWorthSeries
            {
                AccountId = g.Key.AccountId,
                AccountName = g.Key.AccountName,
                AccountType = g.Key.AccountType.ToString(),
                Points = g.OrderBy(ah => ah.Date)
                    .Select(ah => new ValueHistoryPointResponse
                    {
                        Date = ah.Date,
                        Value = ah.Value,
                    })
                    .ToList()
                    .AsReadOnly(),
            })
            .ToList()
            .AsReadOnly();

        return new NetWorthHistoryResponse
        {
            Currency = history.FirstOrDefault()?.Currency ?? "CAD",
            Points = history
                .Select(point => new ValueHistoryPointResponse
                {
                    Date = point.Date,
                    Value = point.Value,
                })
                .ToList()
                .AsReadOnly(),
            Series = series,
        };
    }
}
