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

        var activeHoldings = holdings.Where(h => h.Quantity > 0m).ToList();

        if (activeHoldings.Count == 0)
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

        var symbols = activeHoldings.Select(h => h.Symbol).ToList();
        var latestPrices = await _unitOfWork.PriceHistories.GetLatestBySymbolsAsync(
            symbols,
            cancellationToken
        );

        var missingSymbols = activeHoldings
            .Where(h => ResolvePrice(latestPrices, h.Symbol) is null)
            .Select(h => h.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingSymbols.Count > 0)
        {
            var fetchedAny = await FetchAndPersistMissingPricesAsync(
                missingSymbols,
                cancellationToken
            );
            if (fetchedAny)
            {
                latestPrices = await _unitOfWork.PriceHistories.GetLatestBySymbolsAsync(
                    symbols,
                    cancellationToken
                );
            }
        }

        var holdingContexts = activeHoldings
            .Select(h =>
            {
                var price = ResolvePrice(latestPrices, h.Symbol);
                var resolvedCurrentPrice =
                    price?.Price.Amount
                    ?? (
                        IsCashEquivalentSymbol(h.Symbol)
                            ? h.AverageCostPerUnit.Amount
                            : (decimal?)null
                    );

                var source = ResolvePriceSource(price, h.Symbol);

                var input = new PortfolioPerformanceCalculator.HoldingInput(
                    h.Id,
                    h.Symbol,
                    h.SecurityName,
                    h.Quantity,
                    h.AverageCostPerUnit.Amount,
                    h.AverageCostPerUnit.CurrencyCode,
                    resolvedCurrentPrice,
                    price?.AsOfDate,
                    price?.RecordedAt,
                    h.Lots.Select(l => new PortfolioPerformanceCalculator.LotInput(
                            l.AcquiredDate,
                            l.Quantity,
                            l.UnitCost.Amount,
                            l.Source
                        ))
                        .ToList()
                        .AsReadOnly()
                );

                return new HoldingContext(input, source);
            })
            .ToList();

        var result = PortfolioPerformanceCalculator.Calculate(holdingContexts.Select(c => c.Input));
        var sourceByHoldingId = holdingContexts.ToDictionary(c => c.Input.HoldingId, c => c.Source);

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
                    PriceSource = sourceByHoldingId.GetValueOrDefault(h.HoldingId, "Missing"),
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

    private static bool IsCashEquivalentSymbol(string symbol)
    {
        var normalized = SecuritySymbolMatcher.Normalize(symbol);
        return normalized.StartsWith("CASH", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePriceSource(PriceHistory? price, string symbol)
    {
        if (price is not null)
            return string.IsNullOrWhiteSpace(price.Source) ? "PriceHistory" : price.Source;

        if (IsCashEquivalentSymbol(symbol))
            return "Fallback";

        return "Missing";
    }

    private sealed record HoldingContext(
        PortfolioPerformanceCalculator.HoldingInput Input,
        string Source
    );

    private async Task<bool> FetchAndPersistMissingPricesAsync(
        IReadOnlyList<string> missingSymbols,
        CancellationToken cancellationToken
    )
    {
        var quotes = await _priceFeedProvider.GetLatestPricesAsync(
            missingSymbols,
            cancellationToken
        );
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
            .Select(q => new PriceHistory(
                q.Symbol,
                new Money(q.Price, q.Currency),
                q.AsOfDate,
                _priceFeedProvider.ProviderName
            ))
            .ToList();

        if (newEntries.Count == 0)
            return false;

        await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
