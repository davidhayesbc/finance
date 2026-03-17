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
        if (fromDate > toDate)
            throw new ArgumentOutOfRangeException(nameof(fromDate));

        var accounts = (await _unitOfWork.Accounts.GetByOwnerIdAsync(userId, cancellationToken))
            .Where(a => a.IsActive)
            .ToList();

        if (accounts.Count == 0)
            return [];

        var histories = new List<(Account Account, IReadOnlyList<HistoricalValuePoint> Points)>();
        foreach (var account in accounts)
        {
            var history = await GetAccountHistoryAsync(
                account,
                fromDate,
                toDate,
                cancellationToken
            );
            histories.Add((account, history));
        }

        var fxMaps = await LoadFxRateMapsAsync(
            histories
                .Select(h => h.Account.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(currency =>
                    !string.Equals(currency, baseCurrency, StringComparison.OrdinalIgnoreCase)
                )
                .Select(currency => (FromCurrency: currency, ToCurrency: baseCurrency)),
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

        var points = new List<HistoricalValuePoint>();
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
                {
                    netWorth -= Math.Abs(converted.Value);
                    continue;
                }

                if (assetTypes.Contains(account.AccountType))
                {
                    netWorth += converted.Value;
                    continue;
                }

                netWorth += converted.Value;
            }

            points.Add(
                new HistoricalValuePoint(
                    date,
                    Math.Round(netWorth, 2, MidpointRounding.ToEven),
                    baseCurrency
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

        var priceHistoriesBySecurityId = new Dictionary<Guid, IReadOnlyList<PriceHistory>>();
        foreach (var securityId in holdings.Select(h => h.SecurityId).Distinct())
        {
            var priceHistories = await _unitOfWork.PriceHistories.GetBySecurityIdAsync(
                securityId,
                null,
                toDate,
                cancellationToken
            );
            priceHistoriesBySecurityId[securityId] = priceHistories
                .OrderBy(p => p.AsOfDate)
                .ToList();
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
