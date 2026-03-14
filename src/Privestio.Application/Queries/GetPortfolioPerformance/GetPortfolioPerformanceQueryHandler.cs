using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPortfolioPerformance;

public class GetPortfolioPerformanceQueryHandler
    : IRequestHandler<GetPortfolioPerformanceQuery, PortfolioPerformanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPortfolioPerformanceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PortfolioPerformanceResponse?> Handle(
        GetPortfolioPerformanceQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null || account.OwnerId != request.UserId)
            return null;

        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );

        if (holdings.Count == 0)
        {
            return new PortfolioPerformanceResponse
            {
                AccountId = request.AccountId,
                Currency = account.Currency,
                TotalBookValue = 0m,
                TotalMarketValue = 0m,
                TotalGainLoss = 0m,
                TotalGainLossPercent = 0m,
                PortfolioMoneyWeightedReturn = null,
                CalculatedAt = DateTime.UtcNow,
                Holdings = [],
            };
        }

        var symbols = holdings.Select(h => h.Symbol).ToList();
        var latestPrices = await _unitOfWork.PriceHistories.GetLatestBySymbolsAsync(
            symbols,
            cancellationToken
        );

        var holdingInputs = holdings.Select(h =>
        {
            latestPrices.TryGetValue(h.Symbol, out var price);
            return new PortfolioPerformanceCalculator.HoldingInput(
                h.Id,
                h.Symbol,
                h.SecurityName,
                h.Quantity,
                h.AverageCostPerUnit.Amount,
                h.AverageCostPerUnit.CurrencyCode,
                price?.Price.Amount,
                price?.AsOfDate,
                price?.RecordedAt,
                h.Lots.Select(l => new PortfolioPerformanceCalculator.LotInput(
                        l.AcquiredDate,
                        l.Quantity,
                        l.UnitCost.Amount
                    ))
                    .ToList()
                    .AsReadOnly()
            );
        });

        var result = PortfolioPerformanceCalculator.Calculate(holdingInputs);

        return new PortfolioPerformanceResponse
        {
            AccountId = request.AccountId,
            Currency = account.Currency,
            TotalBookValue = result.TotalBookValue,
            TotalMarketValue = result.TotalMarketValue,
            TotalGainLoss = result.TotalGainLoss,
            TotalGainLossPercent = result.TotalGainLossPercent,
            PortfolioMoneyWeightedReturn = result.PortfolioMoneyWeightedReturn,
            CalculatedAt = result.CalculatedAt,
            Holdings = result
                .Holdings.Select(h => new HoldingPerformanceResponse
                {
                    HoldingId = h.HoldingId,
                    Symbol = h.Symbol,
                    SecurityName = h.SecurityName,
                    Quantity = h.Quantity,
                    CurrentPrice = h.CurrentPrice,
                    Currency = h.Currency,
                    MarketValue = h.MarketValue,
                    BookValue = h.BookValue,
                    GainLoss = h.GainLoss,
                    GainLossPercent = h.GainLossPercent,
                    MoneyWeightedReturn = h.MoneyWeightedReturn,
                    PriceAsOfDate = h.PriceAsOfDate,
                    IsPriceStale = h.IsPriceStale,
                })
                .ToList()
                .AsReadOnly(),
        };
    }
}
