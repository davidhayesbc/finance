using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Domain.Services;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Queries.GetPortfolioPerformance;

public class GetPortfolioPerformanceQueryHandler
    : IRequestHandler<GetPortfolioPerformanceQuery, PortfolioPerformanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;

    public GetPortfolioPerformanceQueryHandler(
        IUnitOfWork unitOfWork,
        IPriceFeedProvider priceFeedProvider
    )
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
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

        var missingSymbols = holdings
            .Where(h => ResolvePrice(latestPrices, h.Symbol) is null)
            .Select(h => h.Symbol)
            .Where(s => !string.Equals(s, "CASH", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingSymbols.Count > 0)
        {
            var fetchedAny = await FetchAndPersistMissingPricesAsync(missingSymbols, cancellationToken);
            if (fetchedAny)
            {
                latestPrices = await _unitOfWork.PriceHistories.GetLatestBySymbolsAsync(
                    symbols,
                    cancellationToken
                );
            }
        }

        var holdingInputs = holdings.Select(h =>
        {
            var price = ResolvePrice(latestPrices, h.Symbol);
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

    private static PriceHistory? ResolvePrice(
        IReadOnlyDictionary<string, PriceHistory> latestPrices,
        string holdingSymbol
    )
    {
        foreach (var candidate in SecuritySymbolMatcher.GetLookupCandidates(holdingSymbol))
        {
            if (latestPrices.TryGetValue(candidate, out var price))
                return price;
        }

        return null;
    }

    private async Task<bool> FetchAndPersistMissingPricesAsync(
        IReadOnlyList<string> missingSymbols,
        CancellationToken cancellationToken
    )
    {
        var quotes = await _priceFeedProvider.GetLatestPricesAsync(missingSymbols, cancellationToken);
        if (quotes.Count == 0)
            return false;

        var keys = quotes
            .Select(q => (SecuritySymbolMatcher.Normalize(q.Symbol), q.AsOfDate))
            .Distinct()
            .ToList();

        var existingKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            keys,
            cancellationToken
        );

        var newEntries = quotes
            .Where(q => q.Price > 0)
            .Where(q =>
                !existingKeys.Contains((SecuritySymbolMatcher.Normalize(q.Symbol), q.AsOfDate))
            )
            .Select(q =>
                new PriceHistory(
                    q.Symbol,
                    new Money(q.Price, q.Currency),
                    q.AsOfDate,
                    _priceFeedProvider.ProviderName
                )
            )
            .ToList();

        if (newEntries.Count == 0)
            return false;

        await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
