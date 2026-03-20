using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.UpdateSecurityDetails;

public class UpdateSecurityDetailsCommandHandler
    : IRequestHandler<UpdateSecurityDetailsCommand, SecurityCatalogItemResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSecurityDetailsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityCatalogItemResponse> Handle(
        UpdateSecurityDetailsCommand request,
        CancellationToken cancellationToken
    )
    {
        var security = await _unitOfWork.Securities.GetByIdAsync(
            request.SecurityId,
            cancellationToken
        );
        if (security is null)
            throw new InvalidOperationException("Security not found.");

        var userHasSecurity = await UserHasSecurityAsync(
            request.UserId,
            request.SecurityId,
            cancellationToken
        );
        if (!userHasSecurity)
            throw new InvalidOperationException("Security not found.");

        security.Rename(request.Name);
        security.UpdateDisplaySymbol(request.DisplaySymbol);
        security.UpdateCurrency(request.Currency);
        security.UpdateExchange(request.Exchange);
        security.SetCashEquivalent(request.IsCashEquivalent);

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var latestBySecurityId = await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
            [security.Id],
            cancellationToken
        );

        latestBySecurityId.TryGetValue(security.Id, out var latestPrice);
        return ToCatalogItem(security, latestPrice);
    }

    private async Task<bool> UserHasSecurityAsync(
        Guid userId,
        Guid securityId,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken);
        if (accounts.Count == 0)
            return false;

        foreach (var account in accounts)
        {
            var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
                account.Id,
                cancellationToken
            );
            if (holdings.Any(h => h.SecurityId == securityId))
                return true;
        }

        return false;
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
