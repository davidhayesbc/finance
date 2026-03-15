using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetPortfolioPerformance;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetPortfolioPerformanceQueryTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IHoldingRepository> _holdings = new();
    private readonly Mock<IPriceHistoryRepository> _prices = new();

    public GetPortfolioPerformanceQueryTests()
    {
        _uow.Setup(x => x.Accounts).Returns(_accounts.Object);
        _uow.Setup(x => x.Holdings).Returns(_holdings.Object);
        _uow.Setup(x => x.PriceHistories).Returns(_prices.Object);
    }

    private GetPortfolioPerformanceQueryHandler CreateHandler() => new(_uow.Object);

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
}
