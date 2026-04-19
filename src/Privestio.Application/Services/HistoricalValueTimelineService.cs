using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Services;

public sealed class HistoricalValueTimelineService
{
    private const string DefaultBaseCurrency = "CAD";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IExchangeRateProvider _exchangeRateProvider;

    public HistoricalValueTimelineService(
        IUnitOfWork unitOfWork,
        IExchangeRateProvider exchangeRateProvider
    )
    {
        _unitOfWork = unitOfWork;
        _exchangeRateProvider = exchangeRateProvider;
    }

    public sealed record HistoricalValuePoint(DateOnly Date, decimal Value, string Currency);

    public sealed record AccountHistoricalValuePoint(
        Guid AccountId,
        string AccountName,
        AccountType AccountType,
        DateOnly Date,
        decimal Value,
        string Currency
    );

    public async Task<IReadOnlyList<HistoricalValuePoint>> GetAccountHistoryAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(account);
        if (fromDate > toDate)
            throw new ArgumentOutOfRangeException(nameof(fromDate));

        return account.AccountType switch
        {
            AccountType.Investment => await BuildInvestmentHistoryAsync(
                account,
                fromDate,
                toDate,
                cancellationToken
            ),
            AccountType.Property => BuildPropertyHistory(account, fromDate, toDate),
            _ => await BuildTransactionalHistoryAsync(account, fromDate, toDate, cancellationToken),
        };
    }

    public async Task<IReadOnlyList<HistoricalValuePoint>> GetNetWorthHistoryAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken,
        string baseCurrency = DefaultBaseCurrency
    )
    {
        var (netWorthPoints, _) = await GetNetWorthHistoryWithSeriesAsync(
            userId,
            fromDate,
            toDate,
            cancellationToken,
            baseCurrency
        );
        return netWorthPoints;
    }

    /// <summary>
    /// Returns both the aggregate net-worth time series and the per-account breakdown in a single
    /// pass, eliminating the double account fetch and double per-account query cost that occurs
    /// when calling <see cref="GetNetWorthHistoryAsync"/> and
    /// <see cref="GetNetWorthHistoryByAccountAsync"/> separately.
    /// </summary>
    public async Task<(
        IReadOnlyList<HistoricalValuePoint> NetWorthPoints,
        IReadOnlyList<AccountHistoricalValuePoint> AccountPoints
    )> GetNetWorthHistoryWithSeriesAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken,
        string baseCurrency = DefaultBaseCurrency
    )
    {
        if (fromDate > toDate)
            throw new ArgumentOutOfRangeException(nameof(fromDate));

        var accounts = (await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken))
            .Where(a => a.IsActive)
            .ToList();

        if (accounts.Count == 0)
            return ([], []);

        // ── Pre-fetch all transactional account transactions in ONE query ──────────────────────
        var transactionalAccountIds = accounts
            .Where(a =>
                a.AccountType != AccountType.Investment && a.AccountType != AccountType.Property
            )
            .Select(a => a.Id)
            .ToList();

        IReadOnlyDictionary<Guid, IReadOnlyList<Transaction>> preloadedTransactions =
            new Dictionary<Guid, IReadOnlyList<Transaction>>();

        if (transactionalAccountIds.Count > 0)
        {
            var earliestOpeningDate = accounts
                .Where(a => transactionalAccountIds.Contains(a.Id))
                .Min(a => a.OpeningDate)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var allTransactions =
                await _unitOfWork.Transactions.GetByAccountIdsAndDateRangeAsync(
                    transactionalAccountIds,
                    earliestOpeningDate,
                    toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                    cancellationToken
                );

            preloadedTransactions = allTransactions
                .GroupBy(t => t.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                        (IReadOnlyList<Transaction>)g.OrderBy(t => t.Date)
                            .ThenBy(t => t.Id)
                            .ToList()
                );
        }

        // ── Pre-fetch all price histories for investment securities in ONE query ──────────────
        var investmentAccounts = accounts
            .Where(a => a.AccountType == AccountType.Investment)
            .ToList();

        IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>> preloadedPriceHistories =
            new Dictionary<Guid, IReadOnlyList<PriceHistory>>();

        IReadOnlyDictionary<Guid, IReadOnlyList<HoldingSnapshot>> preloadedSnapshots =
            new Dictionary<Guid, IReadOnlyList<HoldingSnapshot>>();

        IReadOnlyDictionary<Guid, IReadOnlyList<Holding>> preloadedHoldings =
            new Dictionary<Guid, IReadOnlyList<Holding>>();

        if (investmentAccounts.Count > 0)
        {
            var holdingsByAccount = new Dictionary<Guid, IReadOnlyList<Holding>>();
            var snapshotsByAccount = new Dictionary<Guid, IReadOnlyList<HoldingSnapshot>>();
            var allSecurityIds = new HashSet<Guid>();

            foreach (var inv in investmentAccounts)
            {
                var snapshots = await _unitOfWork.HoldingSnapshots.GetByAccountIdAsync(
                    inv.Id,
                    null,
                    toDate,
                    cancellationToken
                );
                snapshotsByAccount[inv.Id] = snapshots;

                if (snapshots.Count == 0)
                {
                    var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(
                        inv.Id,
                        cancellationToken
                    );
                    holdingsByAccount[inv.Id] = holdings
                        .Where(h => h.Security is not null)
                        .ToList();
                    foreach (var h in holdingsByAccount[inv.Id])
                        allSecurityIds.Add(h.SecurityId);
                }
                else
                {
                    foreach (var s in snapshots)
                        allSecurityIds.Add(s.SecurityId);
                }
            }

            preloadedSnapshots = snapshotsByAccount;
            preloadedHoldings = holdingsByAccount;

            if (allSecurityIds.Count > 0)
            {
                preloadedPriceHistories =
                    await _unitOfWork.PriceHistories.GetBySecurityIdsAsync(
                        allSecurityIds,
                        toDate,
                        cancellationToken
                    );
            }
        }

        // ── Build per-account histories using pre-loaded data ────────────────────────────────
        var histories = new List<(Account Account, IReadOnlyList<HistoricalValuePoint> Points)>(
            accounts.Count
        );

        foreach (var account in accounts)
        {
            var history = await GetAccountHistoryInternalAsync(
                account,
                fromDate,
                toDate,
                preloadedTransactions,
                preloadedPriceHistories,
                preloadedSnapshots,
                preloadedHoldings,
                cancellationToken
            );
            histories.Add((account, history));
        }

        // ── Load ALL required FX rate maps in ONE pass ────────────────────────────────────────
        var allCurrencyPairs = histories
            .Select(h => h.Account.Currency)
            .Concat(
                histories.SelectMany(h => h.Points).Select(p => p.Currency)
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => !string.Equals(c, baseCurrency, StringComparison.OrdinalIgnoreCase))
            .Select(c => (FromCurrency: c, ToCurrency: baseCurrency));

        var fxMaps = await LoadFxRateMapsAsync(
            allCurrencyPairs,
            fromDate,
            toDate,
            cancellationToken
        );

        var assetTypes = new HashSet<AccountType>
        {
            AccountType.Banking,
            AccountType.Investment,
            AccountType.Property,
        };
        var liabilityTypes = new HashSet<AccountType> { AccountType.Credit, AccountType.Loan };

        // ── Aggregate net worth time series ───────────────────────────────────────────────────
        var netWorthPoints = new List<HistoricalValuePoint>();
        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            var netWorth = 0m;
            foreach (var (account, history) in histories)
            {
                var point = history.FirstOrDefault(p => p.Date == date);
                if (point is null)
                    continue;

                var converted = ConvertValue(
                    point.Value,
                    point.Currency,
                    baseCurrency,
                    date,
                    fxMaps
                );
                if (!converted.HasValue)
                    continue;

                if (liabilityTypes.Contains(account.AccountType))
                    netWorth -= Math.Abs(converted.Value);
                else if (assetTypes.Contains(account.AccountType))
                    netWorth += converted.Value;
                else
                    netWorth += converted.Value;
            }

            netWorthPoints.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(netWorth, 2, MidpointRounding.ToEven),
                    baseCurrency
                )
            );
        }

        // ── Flatten per-account breakdown ─────────────────────────────────────────────────────
        var accountPoints = new List<AccountHistoricalValuePoint>();
        foreach (var (account, history) in histories)
        {
            foreach (var point in history)
            {
                var converted = ConvertValue(
                    point.Value,
                    point.Currency,
                    baseCurrency,
                    point.Date,
                    fxMaps
                );

                var displayValue =
                    converted
                    ?? (
                        account.AccountType == AccountType.Credit
                        || account.AccountType == AccountType.Loan
                            ? -Math.Abs(point.Value)
                            : point.Value
                    );

                accountPoints.Add(
                    new AccountHistoricalValuePoint(
                        account.Id,
                        account.Name,
                        account.AccountType,
                        point.Date,
                        Math.Round(displayValue, 2, MidpointRounding.ToEven),
                        baseCurrency
                    )
                );
            }
        }

        return (netWorthPoints.AsReadOnly(), accountPoints.AsReadOnly());
    }

    public async Task<IReadOnlyList<AccountHistoricalValuePoint>> GetNetWorthHistoryByAccountAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken,
        string baseCurrency = DefaultBaseCurrency
    )
    {
        var (_, accountPoints) = await GetNetWorthHistoryWithSeriesAsync(
            userId,
            fromDate,
            toDate,
            cancellationToken,
            baseCurrency
        );
        return accountPoints;
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> GetAccountHistoryInternalAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<Transaction>> preloadedTransactions,
        IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>> preloadedPriceHistories,
        IReadOnlyDictionary<Guid, IReadOnlyList<HoldingSnapshot>> preloadedSnapshots,
        IReadOnlyDictionary<Guid, IReadOnlyList<Holding>> preloadedHoldings,
        CancellationToken cancellationToken
    )
    {
        return account.AccountType switch
        {
            AccountType.Investment => await BuildInvestmentHistoryInternalAsync(
                account,
                fromDate,
                toDate,
                preloadedPriceHistories,
                preloadedSnapshots,
                preloadedHoldings,
                cancellationToken
            ),
            AccountType.Property => BuildPropertyHistory(account, fromDate, toDate),
            _ => BuildTransactionalHistoryFromPreloaded(
                account,
                fromDate,
                toDate,
                preloadedTransactions
            ),
        };
    }

    private static IReadOnlyList<HistoricalValuePoint> BuildTransactionalHistoryFromPreloaded(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<Transaction>> preloadedTransactions
    )
    {
        preloadedTransactions.TryGetValue(account.Id, out var accountTransactions);
        accountTransactions ??= [];

        var points = new List<HistoricalValuePoint>();
        var runningDelta = 0m;
        var txIndex = 0;

        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            while (
                txIndex < accountTransactions.Count
                && DateOnly.FromDateTime(accountTransactions[txIndex].Date) <= date
            )
            {
                runningDelta +=
                    accountTransactions[txIndex].Type == TransactionType.Debit
                        ? -accountTransactions[txIndex].Amount.Amount
                        : accountTransactions[txIndex].Amount.Amount;
                txIndex++;
            }

            var value = account.OpeningBalance.Amount + runningDelta;
            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(value, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildInvestmentHistoryInternalAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>> preloadedPriceHistories,
        IReadOnlyDictionary<Guid, IReadOnlyList<HoldingSnapshot>> preloadedSnapshots,
        IReadOnlyDictionary<Guid, IReadOnlyList<Holding>> preloadedHoldings,
        CancellationToken cancellationToken
    )
    {
        if (
            preloadedSnapshots.TryGetValue(account.Id, out var snapshots) && snapshots.Count > 0
        )
        {
            return await BuildSnapshotBasedHistoryFromPreloadedAsync(
                account,
                snapshots,
                fromDate,
                toDate,
                preloadedPriceHistories,
                cancellationToken
            );
        }

        if (preloadedHoldings.TryGetValue(account.Id, out var holdings))
        {
            return await BuildHoldingBasedHistoryFromPreloadedAsync(
                account,
                holdings,
                fromDate,
                toDate,
                preloadedPriceHistories,
                cancellationToken
            );
        }

        // Fallback to individual DB queries if this account was not pre-loaded.
        return await BuildInvestmentHistoryAsync(account, fromDate, toDate, cancellationToken);
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildSnapshotBasedHistoryFromPreloadedAsync(
        Account account,
        IReadOnlyList<HoldingSnapshot> snapshots,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>> preloadedPriceHistories,
        CancellationToken cancellationToken
    )
    {
        // Group snapshots by statement date. Each statement captures the complete portfolio
        // at that point in time. Using the latest complete statement for each chart date
        // avoids including stale values for securities sold between statements.
        var snapshotsByDate = snapshots
            .GroupBy(s => s.AsOfDate)
            .ToDictionary(g => g.Key, g => g.ToList());

        var statementDates = snapshotsByDate.Keys.OrderBy(d => d).ToList();

        // Use pre-loaded price histories; order ascending by date for binary search.
        var allSecurityIds = snapshots.Select(s => s.SecurityId).Distinct();
        var priceHistoriesBySecurityId = new Dictionary<Guid, IReadOnlyList<PriceHistory>>();
        foreach (var securityId in allSecurityIds)
        {
            if (preloadedPriceHistories.TryGetValue(securityId, out var prices))
                priceHistoriesBySecurityId[securityId] = prices; // already ordered ascending
        }

        var currenciesFromSnapshots = snapshots.Select(s => s.UnitPrice.CurrencyCode);
        var currenciesFromPrices = priceHistoriesBySecurityId
            .Values.SelectMany(values => values)
            .Select(p => p.Price.CurrencyCode);
        var allCurrencies = currenciesFromSnapshots
            .Concat(currenciesFromPrices)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => !string.Equals(c, account.Currency, StringComparison.OrdinalIgnoreCase));

        var fxMaps = await LoadFxRateMapsAsync(
            allCurrencies.Select(c => (FromCurrency: c, ToCurrency: account.Currency)),
            fromDate,
            toDate,
            cancellationToken
        );

        var points = new List<HistoricalValuePoint>();
        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            // Find the latest complete statement on or before this chart date.
            var statementDate = default(DateOnly);
            for (var i = statementDates.Count - 1; i >= 0; i--)
            {
                if (statementDates[i] <= date)
                {
                    statementDate = statementDates[i];
                    break;
                }
            }

            if (statementDate == default)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            var totalValue = 0m;
            foreach (var snapshot in snapshotsByDate[statementDate])
            {
                if (snapshot.Quantity <= 0m)
                    continue;

                decimal priceAmount;
                string priceCurrency;

                if (date == snapshot.AsOfDate)
                {
                    priceAmount = snapshot.UnitPrice.Amount;
                    priceCurrency = snapshot.UnitPrice.CurrencyCode;
                }
                else if (
                    priceHistoriesBySecurityId.TryGetValue(
                        snapshot.SecurityId,
                        out var priceHistories
                    )
                )
                {
                    var price = priceHistories.LastOrDefault(p => p.AsOfDate <= date);
                    if (price is not null)
                    {
                        priceAmount = price.Price.Amount;
                        priceCurrency = price.Price.CurrencyCode;
                    }
                    else
                    {
                        priceAmount = snapshot.UnitPrice.Amount;
                        priceCurrency = snapshot.UnitPrice.CurrencyCode;
                    }
                }
                else
                {
                    priceAmount = snapshot.UnitPrice.Amount;
                    priceCurrency = snapshot.UnitPrice.CurrencyCode;
                }

                var convertedPrice = ConvertValue(
                    priceAmount,
                    priceCurrency,
                    account.Currency,
                    date,
                    fxMaps
                );

                if (!convertedPrice.HasValue)
                    continue;

                totalValue += snapshot.Quantity * convertedPrice.Value;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(totalValue, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildHoldingBasedHistoryFromPreloadedAsync(
        Account account,
        IReadOnlyList<Holding> holdings,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyDictionary<Guid, IReadOnlyList<PriceHistory>> preloadedPriceHistories,
        CancellationToken cancellationToken
    )
    {
        if (holdings.Count == 0)
        {
            return EnumerateDates(fromDate, toDate)
                .Select(date => new HistoricalValuePoint(date, 0m, account.Currency))
                .ToList()
                .AsReadOnly();
        }

        // Use pre-loaded price histories; order ascending by date.
        var priceHistoriesBySecurityId = new Dictionary<Guid, IReadOnlyList<PriceHistory>>();
        foreach (var securityId in holdings.Select(h => h.SecurityId).Distinct())
        {
            if (preloadedPriceHistories.TryGetValue(securityId, out var prices))
                priceHistoriesBySecurityId[securityId] = prices; // already ordered ascending
        }

        var fxMaps = await LoadFxRateMapsAsync(
            priceHistoriesBySecurityId
                .Values.SelectMany(values => values)
                .Select(price => price.Price.CurrencyCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(currency =>
                    !string.Equals(currency, account.Currency, StringComparison.OrdinalIgnoreCase)
                )
                .Select(currency => (FromCurrency: currency, ToCurrency: account.Currency)),
            fromDate,
            toDate,
            cancellationToken
        );

        var points = new List<HistoricalValuePoint>();
        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            var totalValue = 0m;
            foreach (var holding in holdings)
            {
                var quantity = GetQuantityOnDate(holding, date);
                if (quantity <= 0m)
                    continue;

                if (
                    !priceHistoriesBySecurityId.TryGetValue(
                        holding.SecurityId,
                        out var priceHistories
                    )
                )
                    continue;

                var price = priceHistories.LastOrDefault(p => p.AsOfDate <= date);
                if (price is null)
                    continue;

                var convertedPrice = ConvertValue(
                    price.Price.Amount,
                    price.Price.CurrencyCode,
                    account.Currency,
                    date,
                    fxMaps
                );

                if (!convertedPrice.HasValue)
                    continue;

                totalValue += quantity * convertedPrice.Value;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(totalValue, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildTransactionalHistoryAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        var transactions = await _unitOfWork.Transactions.GetByOwnerAndDateRangeAsync(
            account.OwnerId,
            account.OpeningDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            cancellationToken
        );

        var accountTransactions = transactions
            .Where(t => t.AccountId == account.Id)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToList();

        var points = new List<HistoricalValuePoint>();
        var runningDelta = 0m;
        var txIndex = 0;

        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            while (
                txIndex < accountTransactions.Count
                && DateOnly.FromDateTime(accountTransactions[txIndex].Date) <= date
            )
            {
                runningDelta +=
                    accountTransactions[txIndex].Type == TransactionType.Debit
                        ? -accountTransactions[txIndex].Amount.Amount
                        : accountTransactions[txIndex].Amount.Amount;
                txIndex++;
            }

            var value = account.OpeningBalance.Amount + runningDelta;
            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(value, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private static IReadOnlyList<HistoricalValuePoint> BuildPropertyHistory(
        Account account,
        DateOnly fromDate,
        DateOnly toDate
    )
    {
        var valuations = account
            .Valuations.Where(v => !v.IsDeleted)
            .OrderBy(v => v.EffectiveDate)
            .ToList();

        var points = new List<HistoricalValuePoint>();
        var valuationIndex = 0;
        decimal? currentValue = null;

        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            while (
                valuationIndex < valuations.Count
                && valuations[valuationIndex].EffectiveDate <= date
            )
            {
                currentValue = valuations[valuationIndex].EstimatedValue.Amount;
                valuationIndex++;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(
                        currentValue ?? account.OpeningBalance.Amount,
                        2,
                        MidpointRounding.ToEven
                    ),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildInvestmentHistoryAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        // Try snapshot-based history first (created from PDF statement imports).
        var snapshots = await _unitOfWork.HoldingSnapshots.GetByAccountIdAsync(
            account.Id,
            null,
            toDate,
            cancellationToken
        );

        if (snapshots.Count > 0)
        {
            return await BuildSnapshotBasedHistoryAsync(
                account,
                snapshots,
                fromDate,
                toDate,
                cancellationToken
            );
        }

        // Fall back to current holding + lots/price history approach.
        return await BuildHoldingBasedHistoryAsync(account, fromDate, toDate, cancellationToken);
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildSnapshotBasedHistoryAsync(
        Account account,
        IReadOnlyList<HoldingSnapshot> snapshots,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        // Group snapshots by statement date. Each statement captures the complete portfolio
        // at that point in time — only include securities from the latest complete statement
        // for each chart date to avoid stale values from sold/rebalanced securities.
        var snapshotsByDate = snapshots
            .GroupBy(s => s.AsOfDate)
            .ToDictionary(g => g.Key, g => g.ToList());

        var statementDates = snapshotsByDate.Keys.OrderBy(d => d).ToList();

        // Load price histories for interpolation between snapshot dates — batch all at once.
        var allSecurityIds = snapshots.Select(s => s.SecurityId).Distinct();
        var priceHistoriesBySecurityId =
            await _unitOfWork.PriceHistories.GetBySecurityIdsAsync(
                allSecurityIds,
                toDate,
                cancellationToken
            );

        // Collect currencies for FX conversion.
        var currenciesFromSnapshots = snapshots.Select(s => s.UnitPrice.CurrencyCode);
        var currenciesFromPrices = priceHistoriesBySecurityId
            .Values.SelectMany(values => values)
            .Select(p => p.Price.CurrencyCode);
        var allCurrencies = currenciesFromSnapshots
            .Concat(currenciesFromPrices)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => !string.Equals(c, account.Currency, StringComparison.OrdinalIgnoreCase));

        var fxMaps = await LoadFxRateMapsAsync(
            allCurrencies.Select(c => (FromCurrency: c, ToCurrency: account.Currency)),
            fromDate,
            toDate,
            cancellationToken
        );

        var points = new List<HistoricalValuePoint>();
        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            // Find the latest complete statement on or before this chart date.
            var statementDate = default(DateOnly);
            for (var i = statementDates.Count - 1; i >= 0; i--)
            {
                if (statementDates[i] <= date)
                {
                    statementDate = statementDates[i];
                    break;
                }
            }

            if (statementDate == default)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            var totalValue = 0m;
            foreach (var snapshot in snapshotsByDate[statementDate])
            {
                if (snapshot.Quantity <= 0m)
                    continue;

                // Use snapshot's own price on the snapshot date; interpolate with
                // price history for dates between snapshots.
                decimal priceAmount;
                string priceCurrency;

                if (date == snapshot.AsOfDate)
                {
                    priceAmount = snapshot.UnitPrice.Amount;
                    priceCurrency = snapshot.UnitPrice.CurrencyCode;
                }
                else if (
                    priceHistoriesBySecurityId.TryGetValue(
                        snapshot.SecurityId,
                        out var priceHistories
                    )
                )
                {
                    var price = priceHistories.LastOrDefault(p => p.AsOfDate <= date);
                    if (price is not null)
                    {
                        priceAmount = price.Price.Amount;
                        priceCurrency = price.Price.CurrencyCode;
                    }
                    else
                    {
                        // No price history before this date; use snapshot price.
                        priceAmount = snapshot.UnitPrice.Amount;
                        priceCurrency = snapshot.UnitPrice.CurrencyCode;
                    }
                }
                else
                {
                    priceAmount = snapshot.UnitPrice.Amount;
                    priceCurrency = snapshot.UnitPrice.CurrencyCode;
                }

                var convertedPrice = ConvertValue(
                    priceAmount,
                    priceCurrency,
                    account.Currency,
                    date,
                    fxMaps
                );

                if (!convertedPrice.HasValue)
                    continue;

                totalValue += snapshot.Quantity * convertedPrice.Value;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(totalValue, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<IReadOnlyList<HistoricalValuePoint>> BuildHoldingBasedHistoryAsync(
        Account account,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        var holdings = (
            await _unitOfWork.Holdings.GetByAccountIdAsync(account.Id, cancellationToken)
        )
            .Where(h => h.Security is not null)
            .ToList();

        if (holdings.Count == 0)
        {
            return EnumerateDates(fromDate, toDate)
                .Select(date => new HistoricalValuePoint(date, 0m, account.Currency))
                .ToList()
                .AsReadOnly();
        }

        var priceHistoriesBySecurityId =
            await _unitOfWork.PriceHistories.GetBySecurityIdsAsync(
                holdings.Select(h => h.SecurityId).Distinct(),
                toDate,
                cancellationToken
            );

        var fxMaps = await LoadFxRateMapsAsync(
            priceHistoriesBySecurityId
                .Values.SelectMany(values => values)
                .Select(price => price.Price.CurrencyCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(currency =>
                    !string.Equals(currency, account.Currency, StringComparison.OrdinalIgnoreCase)
                )
                .Select(currency => (FromCurrency: currency, ToCurrency: account.Currency)),
            fromDate,
            toDate,
            cancellationToken
        );

        var points = new List<HistoricalValuePoint>();
        foreach (var date in EnumerateDates(fromDate, toDate))
        {
            if (date < account.OpeningDate)
            {
                points.Add(new HistoricalValuePoint(date, 0m, account.Currency));
                continue;
            }

            var totalValue = 0m;
            foreach (var holding in holdings)
            {
                var quantity = GetQuantityOnDate(holding, date);
                if (quantity <= 0m)
                    continue;

                if (
                    !priceHistoriesBySecurityId.TryGetValue(
                        holding.SecurityId,
                        out var priceHistories
                    )
                )
                    continue;

                var price = priceHistories.LastOrDefault(p => p.AsOfDate <= date);
                if (price is null)
                    continue;

                var convertedPrice = ConvertValue(
                    price.Price.Amount,
                    price.Price.CurrencyCode,
                    account.Currency,
                    date,
                    fxMaps
                );

                if (!convertedPrice.HasValue)
                    continue;

                totalValue += quantity * convertedPrice.Value;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(totalValue, 2, MidpointRounding.ToEven),
                    account.Currency
                )
            );
        }

        return points.AsReadOnly();
    }

    private async Task<Dictionary<string, SortedDictionary<DateOnly, decimal>>> LoadFxRateMapsAsync(
        IEnumerable<(string FromCurrency, string ToCurrency)> pairs,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<string, SortedDictionary<DateOnly, decimal>>(
            StringComparer.Ordinal
        );
        var newExchangeRates = new List<ExchangeRate>();

        foreach (var pair in pairs.Distinct())
        {
            var fromCurrency = pair.FromCurrency.Trim().ToUpperInvariant();
            var toCurrency = pair.ToCurrency.Trim().ToUpperInvariant();
            if (fromCurrency == toCurrency)
                continue;

            var key = CreateFxKey(fromCurrency, toCurrency);
            var existing = await _unitOfWork.ExchangeRates.GetAllAsync(
                fromCurrency,
                toCurrency,
                cancellationToken
            );

            var rates = existing
                .Where(rate => rate.AsOfDate <= toDate)
                .GroupBy(rate => rate.AsOfDate)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(r => r.RecordedAt).First().Rate
                );

            if (!rates.Keys.Any(date => date >= fromDate && date <= toDate))
            {
                var fetched = await _exchangeRateProvider.GetHistoricalRatesAsync(
                    fromCurrency,
                    [toCurrency],
                    fromDate,
                    toDate,
                    cancellationToken
                );

                foreach (var quote in fetched.Where(q => q.Rate > 0m))
                {
                    if (rates.ContainsKey(quote.AsOfDate))
                        continue;

                    rates[quote.AsOfDate] = quote.Rate;
                    newExchangeRates.Add(
                        new ExchangeRate(
                            quote.FromCurrency,
                            quote.ToCurrency,
                            quote.Rate,
                            quote.AsOfDate,
                            _exchangeRateProvider.ProviderName
                        )
                    );
                }
            }

            result[key] = new SortedDictionary<DateOnly, decimal>(rates);
        }

        if (newExchangeRates.Count > 0)
        {
            await _unitOfWork.ExchangeRates.AddRangeAsync(newExchangeRates, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static decimal GetQuantityOnDate(Holding holding, DateOnly date)
    {
        if (holding.Lots.Count == 0)
            return date >= DateOnly.FromDateTime(holding.CreatedAt) ? holding.Quantity : 0m;

        return holding.Lots.Where(lot => lot.AcquiredDate <= date).Sum(lot => lot.Quantity);
    }

    private static decimal? ConvertValue(
        decimal value,
        string fromCurrency,
        string toCurrency,
        DateOnly date,
        IReadOnlyDictionary<string, SortedDictionary<DateOnly, decimal>> fxMaps
    )
    {
        var normalizedFrom = fromCurrency.Trim().ToUpperInvariant();
        var normalizedTo = toCurrency.Trim().ToUpperInvariant();

        if (normalizedFrom == normalizedTo)
            return value;

        if (!fxMaps.TryGetValue(CreateFxKey(normalizedFrom, normalizedTo), out var rates))
            return null;

        var rate = rates
            .Where(pair => pair.Key <= date)
            .OrderByDescending(pair => pair.Key)
            .Select(pair => (decimal?)pair.Value)
            .FirstOrDefault();

        return rate.HasValue ? Math.Round(value * rate.Value, 4, MidpointRounding.ToEven) : null;
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly fromDate, DateOnly toDate)
    {
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            yield return date;
    }

    private static string CreateFxKey(string fromCurrency, string toCurrency) =>
        $"{fromCurrency}->{toCurrency}";
}
