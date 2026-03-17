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
    private readonly Mock<IExchangeRateRepository> _exchangeRates = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();
    private readonly SecurityResolutionService _securityResolutionService;

    public GetPortfolioPerformanceQueryTests()
    {
        _uow.Setup(x => x.Accounts).Returns(_accounts.Object);
        _uow.Setup(x => x.Holdings).Returns(_holdings.Object);
        _uow.Setup(x => x.PriceHistories).Returns(_prices.Object);
        _uow.Setup(x => x.ExchangeRates).Returns(_exchangeRates.Object);
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

        _exchangeRateProvider.SetupGet(x => x.ProviderName).Returns("Frankfurter");
        _exchangeRateProvider
            .Setup(x =>
                x.GetLatestRatesAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
        _exchangeRateProvider
            .Setup(x =>
                x.GetHistoricalRatesAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _exchangeRates
            .Setup(x =>
                x.GetLatestByPairAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ExchangeRate?)null);
        _exchangeRates
            .Setup(x =>
                x.GetAllAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
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
        new(
            _uow.Object,
            _priceFeed.Object,
            _exchangeRateProvider.Object,
            _securityResolutionService
        );

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

    [Fact]
    public async Task Handle_UsdQuote_ConvertsPriceToAccountCurrencyUsingStoredFxRate()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var security = SecurityTestHelper.CreateSecurity("EEMV", "Emerging Markets ETF", "USD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            53.6807m,
            new Money(85.85m, "CAD")
        );
        var price = SecurityTestHelper.CreatePriceHistory(
            security,
            65.5650m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Yahoo"
        );
        var fxRate = new ExchangeRate(
            "USD",
            "CAD",
            1.404m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Manual"
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
        _exchangeRates
            .Setup(x => x.GetAllAsync("USD", "CAD", It.IsAny<CancellationToken>()))
            .ReturnsAsync([fxRate]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Currency.Should().Be("CAD");
        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].Currency.Should().Be("CAD");
        result.Holdings[0].QuoteCurrency.Should().Be("USD");
        result.Holdings[0].IsFxConverted.Should().BeTrue();
        result.Holdings[0].FxRateToAccountCurrency.Should().Be(1.404m);
        result.Holdings[0].CurrentPrice.Should().BeApproximately(92.0533m, 0.0001m);
        result.Holdings[0].MarketValue.Should().BeApproximately(4941.49m, 0.01m);

        _exchangeRateProvider.Verify(
            x =>
                x.GetLatestRatesAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_UsdQuoteWithoutFxRate_LeavesMarketValueEmpty()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var security = SecurityTestHelper.CreateSecurity("GSWO", "Low Vol ETF", "USD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            10m,
            new Money(100m, "CAD")
        );
        var price = SecurityTestHelper.CreatePriceHistory(
            security,
            58.05m,
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
        result!.Holdings.Should().HaveCount(1);
        result.Holdings[0].QuoteCurrency.Should().Be("USD");
        result.Holdings[0].IsFxConverted.Should().BeTrue();
        result.Holdings[0].FxRateToAccountCurrency.Should().BeNull();
        result.Holdings[0].CurrentPrice.Should().BeNull();
        result.Holdings[0].MarketValue.Should().BeNull();
        result.Holdings[0].PriceSource.Should().Contain("FX missing");
    }

    [Fact]
    public async Task Handle_UsdQuoteWithMissingRateForAsOfDate_FetchesAndPersistsHistoricalFxRate()
    {
        var userId = Guid.NewGuid();
        var account = MakeAccount(userId);
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        var security = SecurityTestHelper.CreateSecurity("EEMV", "Emerging Markets ETF", "USD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            10m,
            new Money(80m, "CAD")
        );
        var price = SecurityTestHelper.CreatePriceHistory(security, 60m, asOfDate, "Yahoo");

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

        _exchangeRateProvider
            .Setup(x =>
                x.GetHistoricalRatesAsync(
                    "USD",
                    It.Is<IEnumerable<string>>(targets => targets.SequenceEqual(new[] { "CAD" })),
                    asOfDate,
                    asOfDate,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([new ExchangeRateQuote("USD", "CAD", 1.35m, asOfDate)]);

        var result = await CreateHandler()
            .Handle(new GetPortfolioPerformanceQuery(account.Id, userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Holdings.Should().HaveCount(1);
        result.Holdings[0].CurrentPrice.Should().Be(81m);
        result.Holdings[0].MarketValue.Should().Be(810m);
        result.Holdings[0].IsFxConverted.Should().BeTrue();
        result.Holdings[0].FxRateToAccountCurrency.Should().Be(1.35m);

        _exchangeRates.Verify(
            x =>
                x.AddAsync(
                    It.Is<ExchangeRate>(r =>
                        r.FromCurrency == "USD"
                        && r.ToCurrency == "CAD"
                        && r.AsOfDate == asOfDate
                        && r.Rate == 1.35m
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
