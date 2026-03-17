using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly SecurityResolutionService _securityResolutionService;

    public GetAccountsQueryHandler(
        IUnitOfWork unitOfWork,
        IPriceFeedProvider priceFeedProvider,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<IReadOnlyList<AccountResponse>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.OwnerId,
            cancellationToken
        );

        var nonPropertyAndNonInvestmentIds = accounts
            .Where(a =>
                a.AccountType != AccountType.Property && a.AccountType != AccountType.Investment
            )
            .Select(a => a.Id);

        var signedSums = await _unitOfWork.Transactions.GetSignedSumsByAccountIdsAsync(
            nonPropertyAndNonInvestmentIds,
            cancellationToken
        );

        var responses = new List<AccountResponse>(accounts.Count);
        foreach (var account in accounts)
        {
            var balance = await ComputeBalanceAsync(account, signedSums, cancellationToken);
            responses.Add(AccountMapper.ToResponse(account, balance));
        }

        return responses.AsReadOnly();
    }

    private async Task<decimal> ComputeBalanceAsync(
        Account account,
        IReadOnlyDictionary<Guid, decimal> signedSums,
        CancellationToken cancellationToken
    )
    {
        if (account.AccountType == AccountType.Property)
        {
            var latest = account.GetLatestValuation();
            return latest?.EstimatedValue.Amount ?? account.OpeningBalance.Amount;
        }

        if (account.AccountType == AccountType.Investment)
        {
            return await ComputeInvestmentBalanceAsync(account, cancellationToken);
        }

        signedSums.TryGetValue(account.Id, out var sum);
        return account.OpeningBalance.Amount + sum;
    }

    private async Task<decimal> ComputeInvestmentBalanceAsync(
        Account account,
        CancellationToken cancellationToken
    )
    {
        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
            account.Id,
            cancellationToken
        );
        if (holdings.Count == 0)
            return account.CurrentBalance.Amount;

        var symbols = holdings.Select(h => h.SecurityId).Distinct().ToList();
        var latestPrices = await _unitOfWork.PriceHistories.GetLatestBySecurityIdsAsync(
            symbols,
            cancellationToken
        );

        var missingHoldings = holdings
            .Where(h => !latestPrices.ContainsKey(h.SecurityId) && h.Security is not null)
            .ToList();

        var fetchedBySecurityId = new Dictionary<Guid, decimal>();
        if (missingHoldings.Count > 0)
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

            var fetchedQuotes = await _priceFeedProvider.GetLatestPricesAsync(
                lookups,
                cancellationToken
            );

            foreach (var quote in fetchedQuotes.Where(q => q.Price > 0))
            {
                fetchedBySecurityId[quote.SecurityId] = quote.Price;
            }
        }

        var total = holdings.Sum(h =>
        {
            var price = latestPrices.GetValueOrDefault(h.SecurityId);
            var unitPrice =
                price?.Price.Amount
                ?? ResolveFetchedPrice(fetchedBySecurityId, h.SecurityId)
                ?? h.AverageCostPerUnit.Amount;
            return Math.Round(h.Quantity * unitPrice, 2, MidpointRounding.ToEven);
        });

        return total;
    }

    private static decimal? ResolveFetchedPrice(
        IReadOnlyDictionary<Guid, decimal> fetchedBySecurityId,
        Guid securityId
    ) => fetchedBySecurityId.TryGetValue(securityId, out var price) ? price : null;
}
