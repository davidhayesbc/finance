using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetAccountValueHistory;

public class GetAccountValueHistoryQueryHandler
    : IRequestHandler<GetAccountValueHistoryQuery, AccountValueHistoryResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HistoricalValueTimelineService _historicalValueTimelineService;

    public GetAccountValueHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        HistoricalValueTimelineService historicalValueTimelineService
    )
    {
        _unitOfWork = unitOfWork;
        _historicalValueTimelineService = historicalValueTimelineService;
    }

    public async Task<AccountValueHistoryResponse?> Handle(
        GetAccountValueHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetAccessibleByIdAsync(
            request.AccountId,
            request.UserId,
            cancellationToken
        );
        if (account is null)
            return null;

        var history = await _historicalValueTimelineService.GetAccountHistoryAsync(
            account,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );

        return new AccountValueHistoryResponse
        {
            AccountId = account.Id,
            Currency = account.Currency,
            Label = account.AccountType switch
            {
                AccountType.Investment => "Market Value",
                AccountType.Property => "Estimated Value",
                _ => "Balance",
            },
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
