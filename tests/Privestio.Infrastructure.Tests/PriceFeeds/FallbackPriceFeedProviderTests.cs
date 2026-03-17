using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.PriceFeeds;

namespace Privestio.Infrastructure.Tests.PriceFeeds;

public class FallbackPriceFeedProviderTests
{
    private readonly Mock<IPriceFeedProvider> _primary = new();
    private readonly Mock<IPriceFeedProvider> _fallback = new();
    private readonly FallbackPriceFeedProvider _sut;

    public FallbackPriceFeedProviderTests()
    {
        _primary.Setup(p => p.ProviderName).Returns("Primary");
        _fallback.Setup(p => p.ProviderName).Returns("Fallback");

        _sut = new FallbackPriceFeedProvider(
            _primary.Object,
            _fallback.Object,
            NullLogger<FallbackPriceFeedProvider>.Instance
        );
    }

    [Fact]
    public async Task GetLatestPricesAsync_PrimarySucceeds_ReturnsPrimaryResults()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAA"), new(id2, "BBB") };

        var primaryQuotes = new List<PriceQuote>
        {
            new(
                id1,
                "AAA",
                10.0m,
                "CAD",
                DateOnly.FromDateTime(DateTime.UtcNow),
                Source: "YahooFinance"
            ),
            new(
                id2,
                "BBB",
                20.0m,
                "CAD",
                DateOnly.FromDateTime(DateTime.UtcNow),
                Source: "YahooFinance"
            ),
        };

        _primary
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(primaryQuotes.AsReadOnly());

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(primaryQuotes);
        _fallback.Verify(
            f =>
                f.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task GetLatestPricesAsync_PrimaryPartialFail_FallbackFillsGaps()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAA"), new(id2, "BBB") };

        var primaryQuotes = new List<PriceQuote>
        {
            new(
                id1,
                "AAA",
                10.0m,
                "CAD",
                DateOnly.FromDateTime(DateTime.UtcNow),
                Source: "YahooFinance"
            ),
        };

        var fallbackQuotes = new List<PriceQuote>
        {
            new(
                id2,
                "BBB",
                25.0m,
                "USD",
                DateOnly.FromDateTime(DateTime.UtcNow),
                Source: "AlphaVantage"
            ),
        };

        _primary
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(primaryQuotes.AsReadOnly());

        _fallback
            .Setup(f =>
                f.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(l =>
                        l.Count() == 1 && l.First().SecurityId == id2
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(fallbackQuotes.AsReadOnly());

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().HaveCount(2);
        result.Should().Contain(q => q.SecurityId == id1 && q.Price == 10.0m);
        result.Should().Contain(q => q.SecurityId == id2 && q.Price == 25.0m);
    }

    [Fact]
    public async Task GetLatestPricesAsync_PrimaryReturnsEmpty_FallbackCalled()
    {
        var id1 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAA") };

        var fallbackQuotes = new List<PriceQuote>
        {
            new(
                id1,
                "AAA",
                15.0m,
                "CAD",
                DateOnly.FromDateTime(DateTime.UtcNow),
                Source: "AlphaVantage"
            ),
        };

        _primary
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<PriceQuote>().AsReadOnly());

        _fallback
            .Setup(f =>
                f.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(fallbackQuotes.AsReadOnly());

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().HaveCount(1);
        result[0].SecurityId.Should().Be(id1);
        result[0].Price.Should().Be(15.0m);
        _fallback.Verify(
            f =>
                f.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_PrimarySucceeds_ReturnsPrimaryResults()
    {
        var securityId = Guid.NewGuid();
        var lookup = new PriceLookup(securityId, "AAA");
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);

        var primaryQuotes = new List<PriceQuote>
        {
            new(securityId, "AAA", 10.0m, "CAD", new DateOnly(2025, 6, 15), Source: "YahooFinance"),
            new(securityId, "AAA", 11.0m, "CAD", new DateOnly(2025, 6, 16), Source: "YahooFinance"),
        };

        _primary
            .Setup(p => p.GetHistoricalPricesAsync(lookup, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(primaryQuotes.AsReadOnly());

        var result = await _sut.GetHistoricalPricesAsync(lookup, from, to);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(primaryQuotes);
        _fallback.Verify(
            f =>
                f.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_PrimaryReturnsEmpty_FallbackCalled()
    {
        var securityId = Guid.NewGuid();
        var lookup = new PriceLookup(securityId, "AAA");
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);

        var fallbackQuotes = new List<PriceQuote>
        {
            new(securityId, "AAA", 12.0m, "CAD", new DateOnly(2025, 6, 15), Source: "AlphaVantage"),
        };

        _primary
            .Setup(p => p.GetHistoricalPricesAsync(lookup, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceQuote>().AsReadOnly());

        _fallback
            .Setup(f => f.GetHistoricalPricesAsync(lookup, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackQuotes.AsReadOnly());

        var result = await _sut.GetHistoricalPricesAsync(lookup, from, to);

        result.Should().HaveCount(1);
        result[0].Price.Should().Be(12.0m);
        _fallback.Verify(
            f => f.GetHistoricalPricesAsync(lookup, from, to, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void ProviderName_ReturnsFallbackComposite()
    {
        _sut.ProviderName.Should().Be("FallbackComposite");
    }
}
