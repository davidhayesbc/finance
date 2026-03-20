using MediatR;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.SyncHistoricalPrices;

public class SyncHistoricalPricesCommandHandler
    : IRequestHandler<SyncHistoricalPricesCommand, HistoricalPriceSyncResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly PricingOptions _pricingOptions;

    public SyncHistoricalPricesCommandHandler(
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
        SyncHistoricalPricesCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "FromDate must be less than or equal to ToDate."
            );
        }

        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.UserId,
            cancellationToken
        );

        var investmentAccounts = accounts
            .Where(a => a.IsActive && a.AccountType == AccountType.Investment)
            .ToList();

        if (investmentAccounts.Count == 0)
        {
            return new HistoricalPriceSyncResponse
            {
                Provider = _priceFeedProvider.ProviderName,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
            };
        }

        var holdings = new List<Holding>();
        foreach (var account in investmentAccounts)
        {
            var accountHoldings = await _unitOfWork.Holdings.GetByAccountIdAsync(
                account.Id,
                cancellationToken
            );
            holdings.AddRange(
                accountHoldings.Where(h => h.Quantity > 0m && h.Security is not null)
            );
        }

        var securities = holdings
            .Select(h => h.Security)
            .Where(s => s is not null)
            .Cast<Security>()
            .DistinctBy(s => s.Id)
            .ToList();

        if (securities.Count == 0)
        {
            return new HistoricalPriceSyncResponse
            {
                Provider = _priceFeedProvider.ProviderName,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
            };
        }

        var lookups = securities
            .Select(s =>
            {
                var order = s.PricingProviderOrder ?? _pricingOptions.ProviderOrder;
                return _securityResolutionService.BuildPriceLookup(s, order);
            })
            .ToList();

        var allQuotes = new List<PriceQuote>();
        foreach (var lookup in lookups)
        {
            var quotes = await _priceFeedProvider.GetHistoricalPricesAsync(
                lookup,
                request.FromDate,
                request.ToDate,
                cancellationToken
            );
            allQuotes.AddRange(quotes);
        }

        var distinctQuotes = allQuotes
            .Where(q => q.Price > 0m)
            .Where(q => q.AsOfDate >= request.FromDate && q.AsOfDate <= request.ToDate)
            .GroupBy(q => (q.SecurityId, q.AsOfDate))
            .Select(g => g.First())
            .ToList();

        if (distinctQuotes.Count == 0)
        {
            return new HistoricalPriceSyncResponse
            {
                Provider = _priceFeedProvider.ProviderName,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                SecuritiesProcessed = lookups.Count,
            };
        }

        var existingKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            distinctQuotes.Select(q => (q.SecurityId, q.AsOfDate)),
            cancellationToken
        );

        var securityById = securities.ToDictionary(s => s.Id);
        var newEntries = distinctQuotes
            .Where(q => !existingKeys.Contains((q.SecurityId, q.AsOfDate)))
            .Where(q => securityById.ContainsKey(q.SecurityId))
            .Select(q =>
            {
                var security = securityById[q.SecurityId];
                return new PriceHistory(
                    q.SecurityId,
                    security.DisplaySymbol,
                    q.Symbol,
                    new Money(q.Price, q.Currency),
                    q.AsOfDate,
                    q.Source ?? _priceFeedProvider.ProviderName
                );
            })
            .ToList();

        if (newEntries.Count > 0)
        {
            await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new HistoricalPriceSyncResponse
        {
            Provider = _priceFeedProvider.ProviderName,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            SecuritiesProcessed = lookups.Count,
            QuotesFetched = distinctQuotes.Count,
            QuotesInserted = newEntries.Count,
            QuotesSkipped = distinctQuotes.Count - newEntries.Count,
        };
    }
}
