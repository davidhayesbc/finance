using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Queries.GetPortfolioPerformance;

public class GetPortfolioPerformanceQueryHandler
    : IRequestHandler<GetPortfolioPerformanceQuery, PortfolioPerformanceResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly SecurityResolutionService _securityResolutionService;

    public GetPortfolioPerformanceQueryHandler(
        IUnitOfWork unitOfWork,
        IPriceFeedProvider priceFeedProvider,
        IExchangeRateProvider exchangeRateProvider,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
        _exchangeRateProvider = exchangeRateProvider;
        _securityResolutionService = securityResolutionService;
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

        var securityIds = activeHoldings.Select(h => h.SecurityId).ToList();
        var latestPrices = await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
            securityIds,
            cancellationToken
        );

        var missingHoldings = activeHoldings
            .Where(h => !latestPrices.ContainsKey(h.SecurityId) && h.Security is not null)
            .ToList();

        if (missingHoldings.Count > 0)
        {
            var fetchedAny = await FetchAndPersistMissingPricesAsync(
                missingHoldings,
                cancellationToken
            );
            if (fetchedAny)
            {
                latestPrices = await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
                    securityIds,
                    cancellationToken
                );
            }
        }

        var holdingContexts = new List<HoldingContext>(activeHoldings.Count);
        var fxRateCache = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var fxRatesChanged = false;

        foreach (var holding in activeHoldings)
        {
            var price = latestPrices.GetValueOrDefault(holding.SecurityId);
            var source = ResolvePriceSource(price, holding);

            var rawCurrentPrice =
                price?.Price.Amount
                ?? (
                    holding.Security?.IsCashEquivalent == true
                        ? holding.AverageCostPerUnit.Amount
                        : (decimal?)null
                );

            var rawPriceCurrency =
                price?.Price.CurrencyCode
                ?? (
                    holding.Security?.IsCashEquivalent == true
                        ? holding.AverageCostPerUnit.CurrencyCode
                        : null
                );

            decimal? convertedCurrentPrice = null;
            if (rawCurrentPrice.HasValue && !string.IsNullOrWhiteSpace(rawPriceCurrency))
            {
                convertedCurrentPrice = await ConvertPriceToAccountCurrencyAsync(
                    rawCurrentPrice.Value,
                    rawPriceCurrency!,
                    account.Currency,
                    fxRateCache,
                    cancellationToken,
                    changed => fxRatesChanged |= changed
                );

                if (!convertedCurrentPrice.HasValue && price is not null)
                {
                    source = $"{source} (FX missing: {rawPriceCurrency}->{account.Currency})";
                }
            }

            var input = new PortfolioPerformanceCalculator.HoldingInput(
                holding.Id,
                holding.Symbol,
                holding.SecurityName,
                holding.Quantity,
                holding.AverageCostPerUnit.Amount,
                account.Currency,
                convertedCurrentPrice,
                price?.AsOfDate,
                price?.RecordedAt,
                holding.Lots.Select(l => new PortfolioPerformanceCalculator.LotInput(
                        l.AcquiredDate,
                        l.Quantity,
                        l.UnitCost.Amount,
                        l.Source
                    ))
                    .ToList()
                    .AsReadOnly()
            );

            holdingContexts.Add(new HoldingContext(input, source));
        }

        if (fxRatesChanged)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

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

    private static string ResolvePriceSource(PriceHistory? price, Holding holding)
    {
        if (price is not null)
            return string.IsNullOrWhiteSpace(price.Source) ? "PriceHistory" : price.Source;

        if (holding.Security?.IsCashEquivalent == true)
            return "Fallback";

        return "Missing";
    }

    private sealed record HoldingContext(
        PortfolioPerformanceCalculator.HoldingInput Input,
        string Source
    );

    private async Task<bool> FetchAndPersistMissingPricesAsync(
        IReadOnlyList<Holding> missingHoldings,
        CancellationToken cancellationToken
    )
    {
        var lookups = missingHoldings
            .Where(h => h.Security is not null)
            .Select(h => new PriceLookup(
                h.SecurityId,
                _securityResolutionService.GetPreferredPriceLookupSymbol(
                    h.Security!,
                    _priceFeedProvider.ProviderName
                )
            ))
            .DistinctBy(l => l.SecurityId)
            .ToList();

        var quotes = await _priceFeedProvider.GetLatestPricesAsync(lookups, cancellationToken);
        if (quotes.Count == 0)
            return false;

        var keys = quotes.Select(q => (q.SecurityId, q.AsOfDate)).Distinct().ToList();

        var existingKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            keys,
            cancellationToken
        );

        var newEntries = quotes
            .Where(q => q.Price > 0)
            .Where(q => !existingKeys.Contains((q.SecurityId, q.AsOfDate)))
            .Join(
                missingHoldings.Select(h => h.Security!).DistinctBy(s => s.Id),
                quote => quote.SecurityId,
                security => security.Id,
                (q, security) =>
                    new PriceHistory(
                        q.SecurityId,
                        security.DisplaySymbol,
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

    private async Task<decimal?> ConvertPriceToAccountCurrencyAsync(
        decimal price,
        string fromCurrency,
        string toCurrency,
        Dictionary<string, decimal> fxRateCache,
        CancellationToken cancellationToken,
        Action<bool> markChanged
    )
    {
        var normalizedFrom = fromCurrency.Trim().ToUpperInvariant();
        var normalizedTo = toCurrency.Trim().ToUpperInvariant();

        if (normalizedFrom == normalizedTo)
            return price;

        var cacheKey = $"{normalizedFrom}->{normalizedTo}";
        if (!fxRateCache.TryGetValue(cacheKey, out var rate))
        {
            var resolvedRate = await ResolveFxRateAsync(
                normalizedFrom,
                normalizedTo,
                cancellationToken,
                markChanged
            );
            if (!resolvedRate.HasValue)
                return null;

            rate = resolvedRate.Value;
            fxRateCache[cacheKey] = rate;
        }

        return Math.Round(price * rate, 4, MidpointRounding.ToEven);
    }

    private async Task<decimal?> ResolveFxRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken,
        Action<bool> markChanged
    )
    {
        var direct = await _unitOfWork.ExchangeRates.GetLatestByPairAsync(
            fromCurrency,
            toCurrency,
            cancellationToken
        );
        if (direct is not null)
            return direct.Rate;

        var inverse = await _unitOfWork.ExchangeRates.GetLatestByPairAsync(
            toCurrency,
            fromCurrency,
            cancellationToken
        );
        if (inverse is not null)
            return 1m / inverse.Rate;

        var fetched = await _exchangeRateProvider.GetLatestRatesAsync(
            fromCurrency,
            [toCurrency],
            cancellationToken
        );

        var quote = fetched.FirstOrDefault(q =>
            string.Equals(q.FromCurrency, fromCurrency, StringComparison.OrdinalIgnoreCase)
            && string.Equals(q.ToCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)
        );

        if (quote is null || quote.Rate <= 0m)
            return null;

        var existing = await _unitOfWork.ExchangeRates.GetLatestByPairAsync(
            fromCurrency,
            toCurrency,
            cancellationToken
        );

        if (existing is null || existing.AsOfDate != quote.AsOfDate)
        {
            var exchangeRate = new ExchangeRate(
                fromCurrency,
                toCurrency,
                quote.Rate,
                quote.AsOfDate,
                _exchangeRateProvider.ProviderName
            );
            await _unitOfWork.ExchangeRates.AddAsync(exchangeRate, cancellationToken);
            markChanged(true);
        }

        return quote.Rate;
    }
}
