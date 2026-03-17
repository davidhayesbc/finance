using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetPortfolioPerformance;
using Privestio.Application.Services;
using Privestio.Application.Tests;
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
    private readonly SecurityResolutionService _securityResolutionService;

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
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _prices
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(Guid SecurityId, DateOnly AsOfDate)>());

        _securityResolutionService = SecurityTestHelper.CreateSecurityResolutionService(_uow);
    }

    private GetPortfolioPerformanceQueryHandler CreateHandler() =>
        new(_uow.Object, _priceFeed.Object, _securityResolutionService);

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
    public async Task Handle_HoldingsWithStoredPrices_CalculatesGainLoss()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var security = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio"
        );
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            10m,
            new Money(38m, "CAD")
        );
        var price = SecurityTestHelper.CreatePriceHistory(
            security,
            40m,
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
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory> { [security.Id] = price }
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalBookValue.Should().Be(380m);
        result.TotalMarketValue.Should().Be(400m);
        result.TotalGainLoss.Should().Be(20m);
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].PriceSource.Should().Be("Yahoo");
    }

    [Fact]
    public async Task Handle_PricesMissing_UsesProviderLookupSymbolAndPersistsPrice()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var security = SecurityTestHelper.CreateSecurity(
            "KILO.B",
            "Kilo Security",
            "CAD",
            false,
            ("KILO-B.TO", "YahooFinance", true)
        );
        SecurityTestHelper.CreateSecurityResolutionService(_uow, [security]);

        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            10m,
            new Money(38m, "CAD")
        );
        var persistedPrice = SecurityTestHelper.CreatePriceHistory(
            security,
            40m,
            today,
            providerSymbol: "KILO-B.TO"
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _prices
            .SetupSequence(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)new Dictionary<Guid, PriceHistory>()
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory> { [security.Id] = persistedPrice }
            );

        _priceFeed
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(lookups =>
                        lookups.Single().SecurityId == security.Id
                        && lookups.Single().Symbol == "KILO-B.TO"
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([new PriceQuote(security.Id, "KILO-B.TO", 40m, "CAD", today)]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().Be(400m);
        result.Holdings[0].PriceSource.Should().Be("YahooFinance");

        _prices.Verify(
            x =>
                x.AddRangeAsync(
                    It.Is<IEnumerable<PriceHistory>>(entries =>
                        entries.Any(e =>
                            e.SecurityId == security.Id && e.ProviderSymbol == "KILO-B.TO"
                        )
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CashEquivalentWithoutPrice_UsesAverageCostFallback()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var security = SecurityTestHelper.CreateSecurity(
            "CASH.TO",
            "High Interest Savings ETF",
            "CAD",
            true
        );
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            100m,
            new Money(50m, "CAD")
        );

        _accounts
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _prices
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)new Dictionary<Guid, PriceHistory>()
            );

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalMarketValue.Should().Be(5000m);
        result.Holdings[0].CurrentPrice.Should().Be(50m);
        result.Holdings[0].PriceSource.Should().Be("Fallback");
    }
}
