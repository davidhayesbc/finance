using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Queries.GetUserSecurities;

public class GetUserSecuritiesQueryHandler
    : IRequestHandler<GetUserSecuritiesQuery, IReadOnlyList<SecurityCatalogItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSecuritiesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SecurityCatalogItemResponse>> Handle(
        GetUserSecuritiesQuery request,
        CancellationToken cancellationToken
    )
    {
        var securityIds = await GetUserSecurityIdsAsync(request.UserId, cancellationToken);
        if (securityIds.Count == 0)
            return [];

        var securities = await _unitOfWork.Securities.GetByIdsAsync(securityIds, cancellationToken);
        if (securities.Count == 0)
            return [];

        var latestBySecurityId = await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
            securityIds,
            cancellationToken
        );

        return securities
            .Select(security =>
            {
                latestBySecurityId.TryGetValue(security.Id, out var latestPrice);
                return ToCatalogItem(security, latestPrice);
            })
            .OrderBy(s => s.DisplaySymbol, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();
    }

    private async Task<IReadOnlyCollection<Guid>> GetUserSecurityIdsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken);
        if (accounts.Count == 0)
            return [];

        var securityIds = new HashSet<Guid>();

        foreach (var account in accounts)
        {
            var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
                account.Id,
                cancellationToken
            );
            foreach (var holding in holdings)
            {
                securityIds.Add(holding.SecurityId);
            }
        }

        return securityIds;
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
