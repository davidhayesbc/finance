using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetPortfolioPerformance;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetPortfolioPerformanceQueryTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IHoldingRepository> _holdings = new();
    private readonly Mock<IPriceHistoryRepository> _prices = new();
    private readonly Mock<IPriceFeedProvider> _priceFeed = new();

    public GetPortfolioPerformanceQueryTests()
    {
        _uow.Setup(x => x.Accounts).Returns(_accounts.Object);
        _uow.Setup(x => x.Holdings).Returns(_holdings.Object);
        _uow.Setup(x => x.PriceHistories).Returns(_prices.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _priceFeed.SetupGet(x => x.ProviderName).Returns("YahooFinance");
        _priceFeed
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _prices
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(string Symbol, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(string Symbol, DateOnly AsOfDate)>());
    }

    private GetPortfolioPerformanceQueryHandler CreateHandler() =>
        new(_uow.Object, _priceFeed.Object);

    private Account MakeAccount(Guid userId) =>
        new(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        );

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        _accounts
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await CreateHandler()
            .Handle(
                new GetPortfolioPerformanceQuery(Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None
            );

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AccountOwnedByDifferentUser_ReturnsNull()
    {
        var account = MakeAccount(Guid.NewGuid());
        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await CreateHandler()
            .Handle(
                new GetPortfolioPerformanceQuery(account.Id, Guid.NewGuid()),
                CancellationToken.None
            );

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyPortfolio_ReturnsZeroTotals()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalBookValue.Should().Be(0m);
        result.TotalMarketValue.Should().Be(0m);
        result.Holdings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HoldingsWithPrices_CalculatesGainLoss()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var holding = new Holding(
            account.Id,
            "XEQT.TO",
            "iShares Core Equity ETF Portfolio",
            10m,
            new Money(38m, "CAD"),
            null
        );
        var price = new PriceHistory(
            "XEQT.TO",
            new Money(40m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Yahoo"
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _prices
            .Setup(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)
                    new Dictionary<string, PriceHistory> { ["XEQT.TO"] = price }
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalBookValue.Should().Be(380m);
        result.TotalMarketValue.Should().Be(400m);
        result.TotalGainLoss.Should().Be(20m);
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].Symbol.Should().Be("XEQT.TO");
        result.Holdings[0].BookValue.Should().Be(380m);
        result.Holdings[0].MarketValue.Should().Be(400m);
        result.Holdings[0].GainLoss.Should().Be(20m);
        result.Holdings[0].PriceSource.Should().Be("Yahoo");
    }

    [Fact]
    public async Task Handle_HoldingNoPriceHistory_MarketValueIsNull()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var holding = new Holding(
            account.Id,
            "XEQT.TO",
            "iShares Core Equity ETF Portfolio",
            10m,
            new Money(38m, "CAD"),
            null
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _prices
            .Setup(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)new Dictionary<string, PriceHistory>()
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().BeNull();
        result.Holdings[0].CurrentPrice.Should().BeNull();
        result.Holdings[0].MarketValue.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HoldingSymbolWithoutExchangeSuffix_UsesAliasedPriceSymbol()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var holding = new Holding(
            account.Id,
            "XEQT",
            "iShares Core Equity ETF Portfolio",
            10m,
            new Money(38m, "CAD"),
            null
        );
        var aliasedPrice = new PriceHistory(
            "XEQT.TO",
            new Money(40m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Yahoo"
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _prices
            .Setup(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)
                    new Dictionary<string, PriceHistory> { ["XEQT.TO"] = aliasedPrice }
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().Be(400m);
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].Symbol.Should().Be("XEQT");
        result.Holdings[0].CurrentPrice.Should().Be(40m);
        result.Holdings[0].MarketValue.Should().Be(400m);
    }

    [Fact]
    public async Task Handle_PricesMissing_FetchesPricesAndCalculatesMarketValueAndPnL()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var holding = new Holding(
            account.Id,
            "XEQT",
            "iShares Core Equity ETF Portfolio",
            10m,
            new Money(38m, "CAD"),
            null
        );
        var persistedPrice = new PriceHistory("XEQT", new Money(40m, "CAD"), today, "YahooFinance");

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _prices
            .SetupSequence(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)new Dictionary<string, PriceHistory>()
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)
                    new Dictionary<string, PriceHistory> { ["XEQT"] = persistedPrice }
            );

        _priceFeed
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([new PriceQuote("XEQT", 40m, "CAD", today)]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().Be(400m);
        result.TotalGainLoss.Should().Be(20m);
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].CurrentPrice.Should().Be(40m);
        result.Holdings[0].MarketValue.Should().Be(400m);
        result.Holdings[0].GainLoss.Should().Be(20m);
        result.Holdings[0].PriceSource.Should().Be("YahooFinance");

        _prices.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<PriceHistory>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CashEquivalentWithoutQuote_UsesAverageCostForMarketValueAndPnL()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var holding = new Holding(
            account.Id,
            "CASH.TO",
            "Global X High Interest Savings ETF",
            35.0955m,
            new Money(50m, "CAD"),
            null
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _prices
            .Setup(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)new Dictionary<string, PriceHistory>()
            );

        _priceFeed
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().Be(result.TotalBookValue);
        result.TotalGainLoss.Should().Be(0m);
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].CurrentPrice.Should().Be(50m);
        result.Holdings[0].MarketValue.Should().Be(result.Holdings[0].BookValue);
        result.Holdings[0].GainLoss.Should().Be(0m);
        result.Holdings[0].PriceSource.Should().Be("Fallback");
    }

    [Fact]
    public async Task Handle_ZeroQuantityHoldings_AreExcludedFromResponse()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var active = new Holding(account.Id, "XEQT", "iShares Core Equity ETF Portfolio", 10m, new Money(38m, "CAD"), null);
        var closed = new Holding(account.Id, "ZFL", "BMO Long Federal Bond Index ETF", 0m, new Money(20m, "CAD"), null);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([active, closed]);
        _prices
            .Setup(x =>
                x.GetLatestBySymbolsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<string, PriceHistory>)
                    new Dictionary<string, PriceHistory>
                    {
                        ["XEQT"] = new PriceHistory("XEQT", new Money(40m, "CAD"), today, "YahooFinance"),
                    }
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Holdings.Should().HaveCount(1);
        result.Holdings[0].Symbol.Should().Be("XEQT");
    }
}
