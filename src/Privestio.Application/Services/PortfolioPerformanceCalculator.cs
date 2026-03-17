namespace Privestio.Application.Services;

/// <summary>
/// Pure calculation service for portfolio performance metrics.
/// No I/O — accepts pre-fetched data and returns computed results.
/// </summary>
public static class PortfolioPerformanceCalculator
{
    /// <summary>Number of days since AsOfDate before a price is considered stale.</summary>
    public const int PriceStaleThresholdDays = 7;
    private const decimal QuantityTolerance = 0.0001m;

    public record LotInput(
        DateOnly AcquiredDate,
        decimal Quantity,
        decimal UnitCost,
        string? Source = null
    );

    public record HoldingInput(
        Guid HoldingId,
        string Symbol,
        string SecurityName,
        decimal Quantity,
        decimal AverageCostPerUnit,
        string Currency,
        decimal? CurrentPrice,
        DateOnly? PriceAsOfDate,
        DateTime? PriceRecordedAt,
        IReadOnlyList<LotInput> Lots
    );

    public record HoldingResult(
        Guid HoldingId,
        string Symbol,
        string SecurityName,
        decimal Quantity,
        decimal? CurrentPrice,
        string Currency,
        decimal? MarketValue,
        decimal BookValue,
        decimal? GainLoss,
        decimal? GainLossPercent,
        decimal? MoneyWeightedReturn,
        DateOnly? PriceAsOfDate,
        bool IsPriceStale
    );

    public record PortfolioResult(
        decimal TotalBookValue,
        decimal? TotalMarketValue,
        decimal? TotalGainLoss,
        decimal? TotalGainLossPercent,
        decimal? PortfolioMoneyWeightedReturn,
        DateTime CalculatedAt,
        IReadOnlyList<HoldingResult> Holdings
    );

    public static PortfolioResult Calculate(IEnumerable<HoldingInput> holdings)
    {
        var holdingList = holdings.ToList();
        var results = holdingList.Select(CalculateHolding).ToList();

        var totalBook = results.Sum(h => h.BookValue);

        decimal? totalMarket =
            results.Count > 0 && results.All(h => h.MarketValue.HasValue)
                ? results.Sum(h => h.MarketValue!.Value)
                : null;

        decimal? totalGainLoss = totalMarket.HasValue ? totalMarket.Value - totalBook : null;

        decimal? totalGainLossPercent =
            totalGainLoss.HasValue && totalBook != 0
                ? Math.Round(totalGainLoss.Value / totalBook * 100m, 4, MidpointRounding.ToEven)
                : null;

        decimal? portfolioMwr = null;
        if (totalMarket.HasValue)
        {
            var allCashFlows = holdingList
                .SelectMany(h =>
                    GetEffectiveCostLots(h).Select(l => (-l.Quantity * l.UnitCost, l.AcquiredDate))
                )
                .ToList();

            if (allCashFlows.Count > 0)
                portfolioMwr = CalculateXirr(allCashFlows, totalMarket.Value);
        }

        return new PortfolioResult(
            totalBook,
            totalMarket,
            totalGainLoss,
            totalGainLossPercent,
            portfolioMwr,
            DateTime.UtcNow,
            results.AsReadOnly()
        );
    }

    private static HoldingResult CalculateHolding(HoldingInput holding)
    {
        var effectiveCostLots = GetEffectiveCostLots(holding);
        var bookValue = CalculateBookValue(holding, effectiveCostLots);

        decimal? marketValue = null;
        decimal? gainLoss = null;
        decimal? gainLossPercent = null;
        decimal? mwr = null;

        if (holding.CurrentPrice.HasValue)
        {
            marketValue = Math.Round(
                holding.Quantity * holding.CurrentPrice.Value,
                2,
                MidpointRounding.ToEven
            );
            gainLoss = marketValue.Value - bookValue;
            gainLossPercent =
                bookValue != 0
                    ? Math.Round(gainLoss.Value / bookValue * 100m, 4, MidpointRounding.ToEven)
                    : null;

            if (holding.Lots.Count > 0)
            {
                var cashFlows = effectiveCostLots
                    .Select(l => (-l.Quantity * l.UnitCost, l.AcquiredDate))
                    .ToList();

                if (cashFlows.Count > 0)
                    mwr = CalculateXirr(cashFlows, marketValue.Value);
            }
        }

        var isPriceStale =
            holding.PriceAsOfDate.HasValue
            && (
                DateOnly.FromDateTime(DateTime.UtcNow).DayNumber
                - holding.PriceAsOfDate.Value.DayNumber
            ) > PriceStaleThresholdDays;

        return new HoldingResult(
            holding.HoldingId,
            holding.Symbol,
            holding.SecurityName,
            holding.Quantity,
            holding.CurrentPrice,
            holding.Currency,
            marketValue,
            bookValue,
            gainLoss,
            gainLossPercent,
            mwr,
            holding.PriceAsOfDate,
            isPriceStale
        );
    }

    private static decimal CalculateBookValue(HoldingInput holding, IReadOnlyList<LotInput> lots)
    {
        if (lots.Count > 0)
        {
            var effectiveLotQuantity = lots.Sum(l => l.Quantity);

            if (
                IsCashEquivalentSymbol(holding.Symbol)
                || IsQuantityAligned(holding.Quantity, effectiveLotQuantity)
            )
            {
                return Math.Round(
                    lots.Sum(l => l.Quantity * l.UnitCost),
                    2,
                    MidpointRounding.ToEven
                );
            }
        }

        return Math.Round(
            holding.Quantity * holding.AverageCostPerUnit,
            2,
            MidpointRounding.ToEven
        );
    }

    private static bool IsQuantityAligned(decimal holdingQuantity, decimal lotQuantity) =>
        Math.Abs(holdingQuantity - lotQuantity) <= QuantityTolerance;

    private static IReadOnlyList<LotInput> GetEffectiveCostLots(HoldingInput holding)
    {
        if (holding.Lots.Count == 0)
            return holding.Lots;

        if (!IsCashEquivalentSymbol(holding.Symbol))
            return holding.Lots;

        var principalLots = holding.Lots.Where(l => !IsIncomeReinvestmentLot(l.Source)).ToList();
        return principalLots.Count > 0 ? principalLots : holding.Lots;
    }

    private static bool IsCashEquivalentSymbol(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        return normalized.StartsWith("CASH", StringComparison.Ordinal);
    }

    private static bool IsIncomeReinvestmentLot(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        var normalized = source.Trim();
        return normalized.Contains("interest", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("dividend", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("distribution", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("reinvest", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("drip", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates the annualised Money-Weighted Return (XIRR / IRR) using Newton-Raphson.
    /// </summary>
    /// <param name="cashFlows">
    /// Negative amounts represent investments (cash out); positive amounts represent proceeds.
    /// </param>
    /// <param name="terminalValue">Current portfolio value (added as a positive cash flow dated today).</param>
    /// <returns>
    /// The IRR as a percentage (e.g. 12.5 for 12.5%), or <c>null</c> if the calculation
    /// does not converge or is not meaningful.
    /// </returns>
    public static decimal? CalculateXirr(
        IReadOnlyList<(decimal Amount, DateOnly Date)> cashFlows,
        decimal terminalValue
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allFlows = cashFlows.Append((Amount: terminalValue, Date: today)).ToList();

        if (!allFlows.Any(cf => cf.Amount < 0) || !allFlows.Any(cf => cf.Amount > 0))
            return null;

        var baseDate = allFlows.Min(cf => cf.Date);

        double Years(DateOnly d) => (double)(d.DayNumber - baseDate.DayNumber) / 365.25;

        double Npv(double rate) =>
            allFlows.Sum(cf =>
            {
                var t = Years(cf.Date);
                return t == 0 ? (double)cf.Amount : (double)cf.Amount / Math.Pow(1.0 + rate, t);
            });

        double NpvDerivative(double rate) =>
            allFlows.Sum(cf =>
            {
                var t = Years(cf.Date);
                return t == 0 ? 0.0 : -(double)cf.Amount * t / Math.Pow(1.0 + rate, t + 1);
            });

        var guess = 0.1;
        for (var i = 0; i < 100; i++)
        {
            var npv = Npv(guess);
            var derivative = NpvDerivative(guess);

            if (Math.Abs(derivative) < 1e-10)
                break;

            var next = guess - npv / derivative;

            if (double.IsNaN(next) || double.IsInfinity(next))
                break;

            if (Math.Abs(next - guess) < 1e-7)
            {
                guess = next;
                break;
            }

            guess = next;
        }

        if (double.IsNaN(guess) || double.IsInfinity(guess) || guess <= -1.0)
            return null;

        return Math.Round((decimal)guess * 100m, 4, MidpointRounding.ToEven);
    }
}
