using MediatR;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

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
        };
    }
}
