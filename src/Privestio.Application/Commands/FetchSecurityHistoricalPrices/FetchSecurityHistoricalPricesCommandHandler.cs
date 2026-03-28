using MediatR;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.FetchSecurityHistoricalPrices;

public class FetchSecurityHistoricalPricesCommandHandler
    : IRequestHandler<FetchSecurityHistoricalPricesCommand, HistoricalPriceSyncResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly PricingOptions _pricingOptions;

    public FetchSecurityHistoricalPricesCommandHandler(
        IUnitOfWork unitOfWork,
        IPriceFeedProvider priceFeedProvider,
        SecurityResolutionService securityResolutionService,
        IOptions<PricingOptions> pricingOptions
    )
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
        _securityResolutionService = securityResolutionService;
        _pricingOptions = pricingOptions.Value;
    }

    public async Task<HistoricalPriceSyncResponse> Handle(
        FetchSecurityHistoricalPricesCommand request,
        CancellationToken cancellationToken
    )
    {
        var security = await _unitOfWork.Securities.GetByIdAsync(
            request.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new KeyNotFoundException("Security not found.");

        var toDate = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var fromDate =
            request.FromDate
            ?? await _unitOfWork.Lots.GetEarliestAcquiredDateBySecurityIdAsync(
                request.SecurityId,
                cancellationToken
            )
            ?? toDate.AddYears(-10);

        if (fromDate > toDate)
            fromDate = toDate;

        var order = security.PricingProviderOrder ?? _pricingOptions.ProviderOrder;
        var lookup = _securityResolutionService.BuildPriceLookup(security, order);

        var allQuotes = await _priceFeedProvider.GetHistoricalPricesAsync(
            lookup,
            fromDate,
            toDate,
            cancellationToken
        );

        var distinctQuotes = allQuotes
            .Where(q => q.Price > 0m)
            .Where(q => q.AsOfDate >= fromDate && q.AsOfDate <= toDate)
            .GroupBy(q => q.AsOfDate)
            .Select(g => g.First())
            .ToList();

        if (distinctQuotes.Count == 0)
        {
            return new HistoricalPriceSyncResponse
            {
                Provider = _priceFeedProvider.ProviderName,
                FromDate = fromDate,
                ToDate = toDate,
                SecuritiesProcessed = 1,
            };
        }

        var existingKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            distinctQuotes.Select(q => (q.SecurityId, q.AsOfDate)),
            cancellationToken
        );

        var newEntries = distinctQuotes
            .Where(q => !existingKeys.Contains((q.SecurityId, q.AsOfDate)))
            .Select(q => new PriceHistory(
                q.SecurityId,
                security.DisplaySymbol,
                q.Symbol,
                new Money(q.Price, q.Currency),
                q.AsOfDate,
                q.Source ?? _priceFeedProvider.ProviderName
            ))
            .ToList();

        if (newEntries.Count > 0)
        {
            await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new HistoricalPriceSyncResponse
        {
            Provider = _priceFeedProvider.ProviderName,
            FromDate = fromDate,
            ToDate = toDate,
            SecuritiesProcessed = 1,
            QuotesFetched = distinctQuotes.Count,
            QuotesInserted = newEntries.Count,
            QuotesSkipped = distinctQuotes.Count - newEntries.Count,
        };
    }
}
