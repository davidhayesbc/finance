using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Services;

/// <summary>
/// Single authoritative code path for computing an account's current balance.
///
/// Balance derivation rules by account type:
///   Property   — latest non-deleted Valuation by EffectiveDate; falls back to OpeningBalance if no valuations exist.
///   Investment — snapshot-based: sum of latest-per-security HoldingSnapshot MarketValue when snapshots exist (managed funds
///                imported from PDF statements); otherwise live market value from InvestmentPortfolioValuationService;
///                falls back to cached CurrentBalance if portfolio valuation is unavailable.
///   All others — OpeningBalance + signed sum of all non-deleted transactions.
///
/// All query handlers (GetAccountById, GetAccounts, GetNetWorthSummary) MUST use this service
/// instead of implementing their own ComputeBalance logic. Divergence from this service is a bug.
/// </summary>
public sealed class AccountBalanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public AccountBalanceService(
        IUnitOfWork unitOfWork,
        InvestmentPortfolioValuationService investmentPortfolioValuationService
    )
    {
        _unitOfWork = unitOfWork;
        _investmentPortfolioValuationService = investmentPortfolioValuationService;
    }

    /// <summary>
    /// Computes the current balance for a single account, fetching the signed transaction sum
    /// from the database as needed. Use this overload for single-account queries.
    /// For batch scenarios, prefer <see cref="ComputeCurrentBalanceAsync(Account, IReadOnlyDictionary{Guid, decimal}, CancellationToken)"/>.
    /// </summary>
    public async Task<decimal> ComputeCurrentBalanceAsync(
        Account account,
        CancellationToken cancellationToken,
        bool allowExternalPriceFetch = true
    )
    {
        if (account.AccountType == AccountType.Property)
            return ComputePropertyBalance(account);

        if (account.AccountType == AccountType.Investment)
            return await ComputeInvestmentBalanceAsync(account, cancellationToken, allowExternalPriceFetch);

        var signedSum = await _unitOfWork.Transactions.GetSignedSumByAccountIdAsync(
            account.Id,
            cancellationToken
        );
        return account.OpeningBalance.Amount + signedSum;
    }

    /// <summary>
    /// Computes the current balance for a single account using a pre-fetched signed-sums
    /// dictionary. Use this overload in batch scenarios to avoid N+1 database queries for
    /// banking/credit/loan accounts.
    /// </summary>
    public async Task<decimal> ComputeCurrentBalanceAsync(
        Account account,
        IReadOnlyDictionary<Guid, decimal> precomputedSignedSums,
        CancellationToken cancellationToken,
        bool allowExternalPriceFetch = true
    )
    {
        if (account.AccountType == AccountType.Property)
            return ComputePropertyBalance(account);

        if (account.AccountType == AccountType.Investment)
            return await ComputeInvestmentBalanceAsync(account, cancellationToken, allowExternalPriceFetch);

        precomputedSignedSums.TryGetValue(account.Id, out var sum);
        return account.OpeningBalance.Amount + sum;
    }

    // ---------------------------------------------------------------------------
    // Private helpers — the canonical formulas for each account type
    // ---------------------------------------------------------------------------

    private static decimal ComputePropertyBalance(Account account)
    {
        var latest = account.GetLatestValuation();
        return latest?.EstimatedValue.Amount ?? account.OpeningBalance.Amount;
    }

    private async Task<decimal> ComputeInvestmentBalanceAsync(
        Account account,
        CancellationToken cancellationToken,
        bool allowExternalPriceFetch = true
    )
    {
        // Snapshot-based accounts (e.g. managed funds imported from PDF statements) store their
        // complete portfolio value in HoldingSnapshots, not in Holdings + live prices. Use the
        // latest-per-security snapshot total when available — this mirrors the logic in
        // HistoricalValueTimelineService which also prefers snapshots for these accounts.
        var snapshotTotal = await _unitOfWork.HoldingSnapshots.GetCurrentSnapshotTotalAsync(
            account.Id,
            cancellationToken
        );
        if (snapshotTotal.HasValue)
            return snapshotTotal.Value;

        var valuation = await _investmentPortfolioValuationService.CalculateAsync(
            account,
            cancellationToken,
            allowExternalPriceFetch
        );
        return valuation.TotalMarketValue ?? account.CurrentBalance.Amount;
    }
}
