using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.Services;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceFeedProvider _priceFeedProvider;

    public GetAccountsQueryHandler(IUnitOfWork unitOfWork, IPriceFeedProvider priceFeedProvider)
    {
        _unitOfWork = unitOfWork;
        _priceFeedProvider = priceFeedProvider;
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

        var symbols = holdings
            .Select(h => h.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var latestPrices = await _unitOfWork.PriceHistories.GetLatestBySymbolsAsync(
            symbols,
            cancellationToken
        );

        var missingSymbols = holdings
            .Where(h => ResolvePrice(latestPrices, h.Symbol) is null)
            .Select(h => h.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fetchedBySymbol = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (missingSymbols.Count > 0)
        {
            var fetchedQuotes = await _priceFeedProvider.GetLatestPricesAsync(
                missingSymbols,
                cancellationToken
            );

            foreach (var quote in fetchedQuotes.Where(q => q.Price > 0))
            {
                fetchedBySymbol[SecuritySymbolMatcher.Normalize(quote.Symbol)] = quote.Price;
            }
        }

        var total = holdings.Sum(h =>
        {
            var price = ResolvePrice(latestPrices, h.Symbol);
            var unitPrice =
                price?.Price.Amount
                ?? ResolveFetchedPrice(fetchedBySymbol, h.Symbol)
                ?? h.AverageCostPerUnit.Amount;
            return Math.Round(h.Quantity * unitPrice, 2, MidpointRounding.ToEven);
        });

        return total;
    }

    private static PriceHistory? ResolvePrice(
        IReadOnlyDictionary<string, PriceHistory> latestPrices,
        string holdingSymbol
    )
    {
        foreach (var candidate in SecuritySymbolMatcher.GetLookupCandidates(holdingSymbol))
        {
            if (latestPrices.TryGetValue(candidate, out var price))
                return price;
        }

        return null;
    }

    private static decimal? ResolveFetchedPrice(
        IReadOnlyDictionary<string, decimal> fetchedBySymbol,
        string holdingSymbol
    )
    {
        foreach (var candidate in SecuritySymbolMatcher.GetLookupCandidates(holdingSymbol))
        {
            if (fetchedBySymbol.TryGetValue(candidate, out var price))
                return price;
        }

        return null;
    }
}
