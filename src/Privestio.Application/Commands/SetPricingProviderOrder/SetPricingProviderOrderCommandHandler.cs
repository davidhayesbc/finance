using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.SetPricingProviderOrder;

public class SetPricingProviderOrderCommandHandler
    : IRequestHandler<SetPricingProviderOrderCommand, SecurityCatalogItemResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetPricingProviderOrderCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SecurityCatalogItemResponse> Handle(
        SetPricingProviderOrderCommand request,
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

        security.SetPricingProviderOrder(request.ProviderOrder);

        await _unitOfWork.Securities.UpdateAsync(security, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            PricingProviderOrder = security.PricingProviderOrder,
        };
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
}
