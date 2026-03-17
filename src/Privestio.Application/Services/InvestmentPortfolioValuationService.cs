using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Services;

public sealed class InvestmentPortfolioValuationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly SecurityResolutionService _securityResolutionService;

    public InvestmentPortfolioValuationService(
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

    public sealed record InvestmentHoldingValuation(
        Guid HoldingId,
        string Symbol,
        string SecurityName,
        decimal Quantity,
        decimal? CurrentPrice,
        string Currency,
        string QuoteCurrency,
        bool IsFxConverted,
        decimal? FxRateToAccountCurrency,
        decimal? MarketValue,
        decimal BookValue,
        decimal? GainLoss,
        decimal? GainLossPercent,
        decimal? MoneyWeightedReturn,
        DateOnly? PriceAsOfDate,
        bool IsPriceStale,
        string PriceSource
    );

    public sealed record InvestmentPortfolioValuation(
        string Currency,
        decimal TotalBookValue,
        decimal? TotalMarketValue,
        decimal? TotalGainLoss,
        decimal? TotalGainLossPercent,
        decimal? PortfolioMoneyWeightedReturn,
        DateTime CalculatedAt,
        IReadOnlyList<InvestmentHoldingValuation> Holdings
    );

    public async Task<InvestmentPortfolioValuation> CalculateAsync(
        Account account,
        CancellationToken cancellationToken
    )
    {
        if (account.AccountType != AccountType.Investment)
        {
            return new InvestmentPortfolioValuation(
                account.Currency,
                0m,
                0m,
                0m,
                0m,
                null,
                DateTime.UtcNow,
                []
            );
        }

        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
            account.Id,
            cancellationToken
        );
        var activeHoldings = holdings.Where(h => h.Quantity > 0m).ToList();

        if (activeHoldings.Count == 0)
        {
            return new InvestmentPortfolioValuation(
                account.Currency,
                0m,
                0m,
                0m,
                0m,
                null,
                DateTime.UtcNow,
                []
            );
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
            decimal? fxRateToAccountCurrency = null;
            var quoteCurrency = rawPriceCurrency ?? account.Currency;
            var isFxConverted = false;
            if (rawCurrentPrice.HasValue && !string.IsNullOrWhiteSpace(rawPriceCurrency))
            {
                var conversion = await ConvertPriceToAccountCurrencyAsync(
                    rawCurrentPrice.Value,
                    rawPriceCurrency!,
                    account.Currency,
                    price?.AsOfDate,
                    fxRateCache,
                    cancellationToken,
                    changed => fxRatesChanged |= changed
                );
                convertedCurrentPrice = conversion.ConvertedPrice;
                fxRateToAccountCurrency = conversion.FxRateToAccountCurrency;
                isFxConverted = conversion.IsFxConverted;

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
                holding
                    .Lots.Select(l => new PortfolioPerformanceCalculator.LotInput(
                        l.AcquiredDate,
                        l.Quantity,
                        l.UnitCost.Amount,
                        l.Source
                    ))
                    .ToList()
                    .AsReadOnly()
            );

            holdingContexts.Add(
                new HoldingContext(
                    input,
                    source,
                    quoteCurrency,
                    isFxConverted,
                    fxRateToAccountCurrency
                )
            );
        }

        if (fxRatesChanged)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = PortfolioPerformanceCalculator.Calculate(holdingContexts.Select(c => c.Input));
        var sourceByHoldingId = holdingContexts.ToDictionary(c => c.Input.HoldingId, c => c.Source);
        var fxByHoldingId = holdingContexts.ToDictionary(c => c.Input.HoldingId, c => c);

        var valuations = result
            .Holdings.Select(h =>
            {
                var context = fxByHoldingId.GetValueOrDefault(h.HoldingId);
                return new InvestmentHoldingValuation(
                    h.HoldingId,
                    h.Symbol,
                    h.SecurityName,
                    h.Quantity,
                    h.CurrentPrice,
                    h.Currency,
                    context?.QuoteCurrency ?? h.Currency,
                    context?.IsFxConverted ?? false,
                    context?.FxRateToAccountCurrency,
                    h.MarketValue,
                    h.BookValue,
                    h.GainLoss,
                    h.GainLossPercent,
                    h.MoneyWeightedReturn,
                    h.PriceAsOfDate,
                    h.IsPriceStale,
                    sourceByHoldingId.GetValueOrDefault(h.HoldingId, "Missing")
                );
            })
            .ToList()
            .AsReadOnly();

        return new InvestmentPortfolioValuation(
            account.Currency,
            result.TotalBookValue,
            result.TotalMarketValue,
            result.TotalGainLoss,
            result.TotalGainLossPercent,
            result.PortfolioMoneyWeightedReturn,
            result.CalculatedAt,
            valuations
        );
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
        string Source,
        string QuoteCurrency,
        bool IsFxConverted,
        decimal? FxRateToAccountCurrency
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

    private readonly record struct FxConversionResult(
        decimal? ConvertedPrice,
        bool IsFxConverted,
        decimal? FxRateToAccountCurrency
    );

    private async Task<FxConversionResult> ConvertPriceToAccountCurrencyAsync(
        decimal price,
        string fromCurrency,
        string toCurrency,
        DateOnly? asOfDate,
        Dictionary<string, decimal> fxRateCache,
        CancellationToken cancellationToken,
        Action<bool> markChanged
    )
    {
        var normalizedFrom = fromCurrency.Trim().ToUpperInvariant();
        var normalizedTo = toCurrency.Trim().ToUpperInvariant();

        if (normalizedFrom == normalizedTo)
            return new FxConversionResult(price, false, null);

        var asOfKey = asOfDate?.ToString("yyyy-MM-dd") ?? "latest";
        var cacheKey = $"{normalizedFrom}->{normalizedTo}@{asOfKey}";
        if (!fxRateCache.TryGetValue(cacheKey, out var rate))
        {
            var resolvedRate = await ResolveFxRateAsync(
                normalizedFrom,
                normalizedTo,
                asOfDate,
                cancellationToken,
                markChanged
            );
            if (!resolvedRate.HasValue)
                return new FxConversionResult(null, true, null);

            rate = resolvedRate.Value;
            fxRateCache[cacheKey] = rate;
        }

        return new FxConversionResult(
            Math.Round(price * rate, 4, MidpointRounding.ToEven),
            true,
            rate
        );
    }

    private async Task<decimal?> ResolveFxRateAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly? asOfDate,
        CancellationToken cancellationToken,
        Action<bool> markChanged
    )
    {
        var direct = await GetExchangeRateByPairAndDateAsync(
            fromCurrency,
            toCurrency,
            asOfDate,
            cancellationToken
        );
        if (direct is not null)
            return direct.Rate;

        var inverse = await GetExchangeRateByPairAndDateAsync(
            toCurrency,
            fromCurrency,
            asOfDate,
            cancellationToken
        );
        if (inverse is not null)
        {
            var inverseDerivedRate = 1m / inverse.Rate;
            await PersistExchangeRateIfMissingAsync(
                fromCurrency,
                toCurrency,
                inverseDerivedRate,
                inverse.AsOfDate,
                cancellationToken,
                markChanged
            );
            return inverseDerivedRate;
        }

        var quote = await FetchFxQuoteAsync(fromCurrency, toCurrency, asOfDate, cancellationToken);

        if (quote is null || quote.Rate <= 0m)
            return null;

        await PersistExchangeRateIfMissingAsync(
            fromCurrency,
            toCurrency,
            quote.Rate,
            quote.AsOfDate,
            cancellationToken,
            markChanged
        );

        return quote.Rate;
    }

    private async Task<ExchangeRate?> GetExchangeRateByPairAndDateAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly? asOfDate,
        CancellationToken cancellationToken
    )
    {
        if (!asOfDate.HasValue)
        {
            return await _unitOfWork.ExchangeRates.GetLatestByPairAsync(
                fromCurrency,
                toCurrency,
                cancellationToken
            );
        }

        var allRates = await _unitOfWork.ExchangeRates.GetAllAsync(
            fromCurrency,
            toCurrency,
            cancellationToken
        );

        return allRates.FirstOrDefault(r => r.AsOfDate == asOfDate.Value);
    }

    private async Task<ExchangeRateQuote?> FetchFxQuoteAsync(
        string fromCurrency,
        string toCurrency,
        DateOnly? asOfDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ExchangeRateQuote> fetched;
        if (asOfDate.HasValue)
        {
            fetched = await _exchangeRateProvider.GetHistoricalRatesAsync(
                fromCurrency,
                [toCurrency],
                asOfDate.Value,
                asOfDate.Value,
                cancellationToken
            );
        }
        else
        {
            fetched = await _exchangeRateProvider.GetLatestRatesAsync(
                fromCurrency,
                [toCurrency],
                cancellationToken
            );
        }

        return fetched.FirstOrDefault(q =>
            string.Equals(q.FromCurrency, fromCurrency, StringComparison.OrdinalIgnoreCase)
            && string.Equals(q.ToCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task PersistExchangeRateIfMissingAsync(
        string fromCurrency,
        string toCurrency,
        decimal rate,
        DateOnly asOfDate,
        CancellationToken cancellationToken,
        Action<bool> markChanged
    )
    {
        var existing = await GetExchangeRateByPairAndDateAsync(
            fromCurrency,
            toCurrency,
            asOfDate,
            cancellationToken
        );

        if (existing is null)
        {
            var exchangeRate = new ExchangeRate(
                fromCurrency,
                toCurrency,
                rate,
                asOfDate,
                _exchangeRateProvider.ProviderName
            );
            await _unitOfWork.ExchangeRates.AddAsync(exchangeRate, cancellationToken);
            markChanged(true);
        }
    }
}
