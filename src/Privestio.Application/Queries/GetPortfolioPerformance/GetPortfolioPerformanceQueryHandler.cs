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
    private readonly SecurityResolutionService _securityResolutionService;

    public GetPortfolioPerformanceQueryHandler(
        IUnitOfWork unitOfWork,
        IPriceFeedProvider priceFeedProvider,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
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

        var holdingContexts = activeHoldings
            .Select(h =>
            {
                var price = latestPrices.GetValueOrDefault(h.SecurityId);
                var resolvedCurrentPrice =
                    price?.Price.Amount
                    ?? (
                        h.Security?.IsCashEquivalent == true
                            ? h.AverageCostPerUnit.Amount
                            : (decimal?)null
                    );

                var source = ResolvePriceSource(price, h);

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
}
