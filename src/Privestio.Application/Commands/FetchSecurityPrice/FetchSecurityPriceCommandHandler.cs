using MediatR;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.FetchSecurityPrice;

public class FetchSecurityPriceCommandHandler
    : IRequestHandler<FetchSecurityPriceCommand, SecurityCatalogItemResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly PricingOptions _pricingOptions;

    public FetchSecurityPriceCommandHandler(
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

    public async Task<SecurityCatalogItemResponse> Handle(
        FetchSecurityPriceCommand request,
        CancellationToken cancellationToken
    )
    {
        var security = await _unitOfWork.Securities.GetByIdAsync(
            request.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new InvalidOperationException("Security not found.");

        var order = security.PricingProviderOrder ?? _pricingOptions.ProviderOrder;
        var lookup = _securityResolutionService.BuildPriceLookup(security, order);

        var quotes = await _priceFeedProvider.GetLatestPricesAsync([lookup], cancellationToken);

        PriceHistory? latestPrice = null;

        if (quotes.Count > 0)
        {
            var quote = quotes[0];
            var alreadyExists = await _unitOfWork.PriceHistories.ExistsBySecurityIdAndDateAsync(
                quote.SecurityId,
                quote.AsOfDate,
                cancellationToken
            );

            if (!alreadyExists)
            {
                latestPrice = new PriceHistory(
                    quote.SecurityId,
                    security.DisplaySymbol,
                    quote.Symbol,
                    new Money(quote.Price, quote.Currency),
                    quote.AsOfDate,
                    quote.Source ?? "Unknown"
                );
                await _unitOfWork.PriceHistories.AddAsync(latestPrice, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        latestPrice ??= (
            await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
                [security.Id],
                cancellationToken
            )
        ).GetValueOrDefault(security.Id);

        return ToCatalogItem(security, latestPrice);
    }

    private static SecurityCatalogItemResponse ToCatalogItem(
        Security security,
        PriceHistory? latestPrice
    )
    {
        return new SecurityCatalogItemResponse
        {
            Id = security.Id,
            CanonicalSymbol = security.CanonicalSymbol,
            DisplaySymbol = security.DisplaySymbol,
            Name = security.Name,
            Currency = security.Currency,
            Exchange = security.Exchange,
            IsCashEquivalent = security.IsCashEquivalent,
            Aliases = security.Aliases.Select(SecurityAliasMapper.ToResponse).ToList(),
            Identifiers = security.Identifiers.Select(SecurityIdentifierMapper.ToResponse).ToList(),
            LatestPrice = latestPrice?.Price.Amount,
            LatestPriceCurrency = latestPrice?.Price.CurrencyCode,
            LatestPriceAsOfDate = latestPrice?.AsOfDate,
            LatestPriceSource = latestPrice?.Source,
            LatestProviderSymbol = latestPrice?.ProviderSymbol,
            PricingProviderOrder = security.PricingProviderOrder,
        };
    }
}
