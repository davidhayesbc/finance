using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.PriceFeeds;

namespace Privestio.Infrastructure.Tests.PriceFeeds;

public class ChainedPriceFeedProviderTests
{
    private readonly Mock<IPriceFeedProvider> _yahoo = new();
    private readonly Mock<IPriceFeedProvider> _msn = new();
    private readonly ChainedPriceFeedProvider _sut;

    public ChainedPriceFeedProviderTests()
    {
        _yahoo.Setup(p => p.ProviderName).Returns("YahooFinance");
        _msn.Setup(p => p.ProviderName).Returns("MsnFinance");

        var providers = new Dictionary<string, IPriceFeedProvider>
        {
            ["YahooFinance"] = _yahoo.Object,
            ["MsnFinance"] = _msn.Object,
        };

        _sut = new ChainedPriceFeedProvider(
            providers,
            ["YahooFinance", "MsnFinance"],
            NullLogger<ChainedPriceFeedProvider>.Instance
        );
    }

    [Fact]
    public async Task GetLatestPricesAsync_TriesProvidersInOrder()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAA"), new(id2, "BBB") };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var yahooQuotes = new List<PriceQuote>
        {
            new(id1, "AAA", 10.0m, "CAD", today, Source: "YahooFinance"),
        };
        var msnQuotes = new List<PriceQuote>
        {
            new(id2, "BBB", 25.0m, "USD", today, Source: "MsnFinance"),
        };

        _yahoo
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(yahooQuotes.AsReadOnly());

        _msn.Setup(p =>
                p.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(l =>
                        l.Count() == 1 && l.First().SecurityId == id2
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(msnQuotes.AsReadOnly());

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().HaveCount(2);
        result.Should().Contain(q => q.SecurityId == id1 && q.Price == 10.0m);
        result.Should().Contain(q => q.SecurityId == id2 && q.Price == 25.0m);
    }

    [Fact]
    public async Task GetLatestPricesAsync_SkipsResolvedSecurities()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAA"), new(id2, "BBB") };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var yahooQuotes = new List<PriceQuote>
        {
            new(id1, "AAA", 10.0m, "CAD", today, Source: "YahooFinance"),
            new(id2, "BBB", 20.0m, "CAD", today, Source: "YahooFinance"),
        };

        _yahoo
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(yahooQuotes.AsReadOnly());

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().HaveCount(2);
        _msn.Verify(
            f =>
                f.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task GetLatestPricesAsync_RemapsSymbolsUsingProviderSymbols()
    {
        var id1 = Guid.NewGuid();
        var providerSymbols = new Dictionary<string, string>
        {
            ["YahooFinance"] = "XEQT.TO",
            ["MsnFinance"] = "XEQT:XTSE",
        };
        var lookups = new List<PriceLookup> { new(id1, "XEQT", providerSymbols) };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _yahoo
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(l => l.First().Symbol == "XEQT.TO"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new List<PriceQuote>
                {
                    new(id1, "XEQT.TO", 30.0m, "CAD", today, Source: "YahooFinance"),
                }.AsReadOnly()
            );

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().ContainSingle(q => q.SecurityId == id1 && q.Symbol == "XEQT.TO");
    }

    [Fact]
    public async Task GetLatestPricesAsync_FallsBackToDefaultSymbolWhenNoProviderMapping()
    {
        var id1 = Guid.NewGuid();
        var lookups = new List<PriceLookup> { new(id1, "AAPL") };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _yahoo
            .Setup(p =>
                p.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(l => l.First().Symbol == "AAPL"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new List<PriceQuote>
                {
                    new(id1, "AAPL", 150.0m, "USD", today, Source: "YahooFinance"),
                }.AsReadOnly()
            );

        var result = await _sut.GetLatestPricesAsync(lookups);

        result.Should().ContainSingle(q => q.Symbol == "AAPL");
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_TriesProvidersInOrder()
    {
        var securityId = Guid.NewGuid();
        var lookup = new PriceLookup(securityId, "AAA");
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);

        _yahoo
            .Setup(p =>
                p.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    from,
                    to,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<PriceQuote>().AsReadOnly());

        var msnQuotes = new List<PriceQuote>
        {
            new(securityId, "AAA", 12.0m, "CAD", new DateOnly(2025, 6, 15), Source: "MsnFinance"),
        };
        _msn.Setup(p =>
                p.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    from,
                    to,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(msnQuotes.AsReadOnly());

        var result = await _sut.GetHistoricalPricesAsync(lookup, from, to);

        result.Should().HaveCount(1);
        result[0].Price.Should().Be(12.0m);
    }

    [Fact]
    public async Task GetHistoricalPricesAsync_RemapsSymbolsForProvider()
    {
        var securityId = Guid.NewGuid();
        var providerSymbols = new Dictionary<string, string> { ["YahooFinance"] = "VFV.TO" };
        var lookup = new PriceLookup(securityId, "VFV", providerSymbols);
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);

        _yahoo
            .Setup(p =>
                p.GetHistoricalPricesAsync(
                    It.Is<PriceLookup>(l => l.Symbol == "VFV.TO"),
                    from,
                    to,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new List<PriceQuote>
                {
                    new(securityId, "VFV.TO", 40.0m, "CAD", new DateOnly(2025, 6, 15)),
                }.AsReadOnly()
            );

        var result = await _sut.GetHistoricalPricesAsync(lookup, from, to);

        result.Should().ContainSingle(q => q.Symbol == "VFV.TO");
    }

    [Fact]
    public void ProviderName_ReturnsChained()
    {
        _sut.ProviderName.Should().Be("Chained");
    }
}
