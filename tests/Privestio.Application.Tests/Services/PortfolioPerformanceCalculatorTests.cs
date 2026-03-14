using Privestio.Application.Services;

namespace Privestio.Application.Tests.Services;

public class PortfolioPerformanceCalculatorTests
{
    private static PortfolioPerformanceCalculator.LotInput Lot(
        int daysAgo,
        decimal qty,
        decimal cost
    ) => new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)), qty, cost);

    private static PortfolioPerformanceCalculator.HoldingInput Holding(
        string symbol,
        decimal qty,
        decimal avgCost,
        decimal? currentPrice,
        DateOnly? priceAsOf = null,
        DateTime? priceRecordedAt = null,
        params PortfolioPerformanceCalculator.LotInput[] lots
    ) =>
        new(
            Guid.NewGuid(),
            symbol,
            $"{symbol} Security",
            qty,
            avgCost,
            "CAD",
            currentPrice,
            priceAsOf ?? (currentPrice.HasValue ? DateOnly.FromDateTime(DateTime.UtcNow) : null),
            priceRecordedAt ?? (currentPrice.HasValue ? DateTime.UtcNow : null),
            lots
        );

    // ── Basic gain/loss calculations ──────────────────────────────────────────

    [Fact]
    public void Calculate_WithNoPrices_TotalMarketValueIsNull()
    {
        var result = PortfolioPerformanceCalculator.Calculate([
            Holding("ABC", 10m, 50m, null, lots: Lot(30, 10m, 50m)),
        ]);

        result.TotalBookValue.Should().Be(500m);
        result.TotalMarketValue.Should().BeNull();
        result.TotalGainLoss.Should().BeNull();
    }

    [Fact]
    public void Calculate_WithCurrentPrice_CalculatesGainLoss()
    {
        var result = PortfolioPerformanceCalculator.Calculate([
            Holding("ABC", 10m, 50m, 60m, lots: Lot(365, 10m, 50m)),
        ]);

        result.TotalBookValue.Should().Be(500m);
        result.TotalMarketValue.Should().Be(600m);
        result.TotalGainLoss.Should().Be(100m);
        result.TotalGainLossPercent.Should().Be(20m);
    }

    [Fact]
    public void Calculate_MultipleHoldings_SumsCorrectly()
    {
        var result = PortfolioPerformanceCalculator.Calculate([
            Holding("ABC", 10m, 50m, 60m, lots: Lot(365, 10m, 50m)),
            Holding("XYZ", 5m, 100m, 80m, lots: Lot(365, 5m, 100m)),
        ]);

        // ABC: book=500, market=600  |  XYZ: book=500, market=400
        result.TotalBookValue.Should().Be(1000m);
        result.TotalMarketValue.Should().Be(1000m);
        result.TotalGainLoss.Should().Be(0m);
        result.TotalGainLossPercent.Should().Be(0m);
        result.Holdings.Should().HaveCount(2);
    }

    [Fact]
    public void Calculate_OnePriceMissing_TotalMarketValueIsNull()
    {
        // One holding has price, the other doesn't → can't sum market value
        var result = PortfolioPerformanceCalculator.Calculate([
            Holding("ABC", 10m, 50m, 55m, lots: Lot(30, 10m, 50m)),
            Holding("XYZ", 5m, 100m, null, lots: Lot(30, 5m, 100m)),
        ]);

        result.TotalMarketValue.Should().BeNull();
    }

    [Fact]
    public void Calculate_ZeroBookValue_HoldingGainLossPercentIsNull()
    {
        var result = PortfolioPerformanceCalculator.Calculate([
            Holding("ABC", 10m, 0m, 60m, lots: Lot(30, 10m, 0m)),
        ]);

        result.Holdings[0].GainLossPercent.Should().BeNull();
    }

    [Fact]
    public void Calculate_EmptyHoldings_ReturnsZeroTotals()
    {
        var result = PortfolioPerformanceCalculator.Calculate([]);

        result.TotalBookValue.Should().Be(0m);
        result.TotalMarketValue.Should().BeNull();
        result.Holdings.Should().BeEmpty();
    }

    // ── Stale-price detection ─────────────────────────────────────────────────

    [Fact]
    public void IsPriceStale_WhenAsOfDateIsRecent_ReturnsFalse()
    {
        var h = Holding(
            "ABC",
            10m,
            50m,
            55m,
            priceAsOf: DateOnly.FromDateTime(DateTime.UtcNow),
            priceRecordedAt: DateTime.UtcNow,
            Lot(30, 10m, 50m)
        );

        PortfolioPerformanceCalculator.Calculate([h]).Holdings[0].IsPriceStale.Should().BeFalse();
    }

    [Fact]
    public void IsPriceStale_WhenAsOfDateOlderThanThreshold_ReturnsTrue()
    {
        var staleDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(-(PortfolioPerformanceCalculator.PriceStaleThresholdDays + 1))
        );
        var h = Holding(
            "ABC",
            10m,
            50m,
            55m,
            priceAsOf: staleDate,
            priceRecordedAt: DateTime.UtcNow,
            Lot(60, 10m, 50m)
        );

        PortfolioPerformanceCalculator.Calculate([h]).Holdings[0].IsPriceStale.Should().BeTrue();
    }

    [Fact]
    public void IsPriceStale_WhenNoPriceAvailable_ReturnsFalse()
    {
        var h = Holding("ABC", 10m, 50m, null, lots: Lot(30, 10m, 50m));

        PortfolioPerformanceCalculator.Calculate([h]).Holdings[0].IsPriceStale.Should().BeFalse();
    }

    // ── XIRR (Money-Weighted Return) ──────────────────────────────────────────

    [Fact]
    public void CalculateXirr_PositiveReturn_ReturnsPositiveRate()
    {
        // $1 000 invested 365 days ago, now worth $1 100 ≈ +10 %
        var flows = new List<(decimal, DateOnly)>
        {
            (-1000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365))),
        };

        var rate = PortfolioPerformanceCalculator.CalculateXirr(flows, 1100m);

        rate.Should().NotBeNull();
        rate!.Value.Should().BeApproximately(10m, 0.5m);
    }

    [Fact]
    public void CalculateXirr_NegativeReturn_ReturnsNegativeRate()
    {
        // $1 000 invested 365 days ago, now worth $900 ≈ −10 %
        var flows = new List<(decimal, DateOnly)>
        {
            (-1000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365))),
        };

        var rate = PortfolioPerformanceCalculator.CalculateXirr(flows, 900m);

        rate.Should().NotBeNull();
        rate!.Value.Should().BeApproximately(-10m, 0.5m);
    }

    [Fact]
    public void CalculateXirr_NoPositiveTerminalValue_ReturnsNull()
    {
        var flows = new List<(decimal, DateOnly)>
        {
            (-1000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365))),
        };

        // Terminal value also negative — no positive cash flow → null
        var rate = PortfolioPerformanceCalculator.CalculateXirr(flows, -100m);

        rate.Should().BeNull();
    }

    [Fact]
    public void CalculateXirr_MultipleLots_ConvergesCorrectly()
    {
        // Two buys: $500 at 365 days ago + $500 at 180 days ago = $1000 total cost
        // Current value $1200 → positive return
        var flows = new List<(decimal, DateOnly)>
        {
            (-500m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365))),
            (-500m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-180))),
        };

        var rate = PortfolioPerformanceCalculator.CalculateXirr(flows, 1200m);

        rate.Should().NotBeNull();
        rate!.Value.Should().BeGreaterThan(0m);
    }
}
