using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetNetWorthSummary;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class NetWorthSummaryQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IHoldingRepository> _holdingRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IPriceFeedProvider> _priceFeedProviderMock;
    private readonly Mock<IExchangeRateRepository> _exchangeRateRepositoryMock;
    private readonly Mock<IExchangeRateProvider> _exchangeRateProviderMock;
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public NetWorthSummaryQueryTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _holdingRepositoryMock = new Mock<IHoldingRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _priceFeedProviderMock = new Mock<IPriceFeedProvider>();
        _exchangeRateRepositoryMock = new Mock<IExchangeRateRepository>();
        _exchangeRateProviderMock = new Mock<IExchangeRateProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.SetupGet(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Transactions).Returns(_transactionRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Holdings).Returns(_holdingRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.PriceHistories).Returns(_priceHistoryRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.ExchangeRates).Returns(_exchangeRateRepositoryMock.Object);

        _securityResolutionService = SecurityTestHelper.CreateSecurityResolutionService(
            _unitOfWorkMock
        );
        _investmentPortfolioValuationService = new InvestmentPortfolioValuationService(
            _unitOfWorkMock.Object,
            _priceFeedProviderMock.Object,
            _exchangeRateProviderMock.Object,
            _securityResolutionService,
            Options.Create(new PricingOptions())
        );

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        _priceHistoryRepositoryMock
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)new Dictionary<Guid, PriceHistory>()
            );

        _exchangeRateRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private GetNetWorthSummaryQueryHandler CreateHandler() =>
        new(_unitOfWorkMock.Object, new AccountBalanceService(_unitOfWorkMock.Object, _investmentPortfolioValuationService));

    [Fact]
    public async Task GetNetWorthSummary_WithMixedAccounts_CalculatesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chequing = CreateAccount(
            userId,
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            5000m
        );
        var rrsp = CreateAccount(
            userId,
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            0.01m
        );
        var creditCard = CreateAccount(
            userId,
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            -2000m
        );
        var mortgage = CreateAccount(
            userId,
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            -300000m
        );
        var security = SecurityTestHelper.CreateSecurity(
            "VBAL",
            "Vanguard Balanced ETF Portfolio"
        );
        var holding = SecurityTestHelper.CreateHolding(
            rrsp.Id,
            security,
            1000m,
            new Money(45m, "CAD")
        );

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { chequing, rrsp, creditCard, mortgage });

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(rrsp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _priceHistoryRepositoryMock
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory>
                    {
                        [security.Id] = SecurityTestHelper.CreatePriceHistory(
                            security,
                            50m,
                            DateOnly.FromDateTime(DateTime.UtcNow)
                        ),
                    }
            );

        var handler = CreateHandler();
        var query = new GetNetWorthSummaryQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalAssets.Should().Be(55000m); // 5000 + 50000
        result.TotalLiabilities.Should().Be(302000m); // abs(-2000) + abs(-300000)
        result.NetWorth.Should().Be(55000m - 302000m);
        result.Currency.Should().Be("CAD");
        result.AccountSummaries.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetNetWorthSummary_NoAccounts_ReturnsZeros()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var handler = CreateHandler();
        var query = new GetNetWorthSummaryQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalAssets.Should().Be(0m);
        result.TotalLiabilities.Should().Be(0m);
        result.NetWorth.Should().Be(0m);
        result.AssetAllocation.Should().BeEmpty();
        result.AccountSummaries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNetWorthSummary_AssetAllocation_SumsTo100Percent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chequing = CreateAccount(
            userId,
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            20000m
        );
        var rrsp = CreateAccount(
            userId,
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            0.01m
        );
        var security = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio"
        );
        var holding = SecurityTestHelper.CreateHolding(
            rrsp.Id,
            security,
            2000m,
            new Money(35m, "CAD")
        );

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { chequing, rrsp });

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(rrsp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _priceHistoryRepositoryMock
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory>
                    {
                        [security.Id] = SecurityTestHelper.CreatePriceHistory(
                            security,
                            40m,
                            DateOnly.FromDateTime(DateTime.UtcNow)
                        ),
                    }
            );

        var handler = CreateHandler();
        var query = new GetNetWorthSummaryQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.AssetAllocation.Should().HaveCount(2);
        var totalPercentage = result.AssetAllocation.Sum(a => a.Percentage);
        totalPercentage.Should().Be(100m);

        var banking = result.AssetAllocation.First(a => a.AccountType == "Banking");
        banking.Percentage.Should().Be(20m);

        var investment = result.AssetAllocation.First(a => a.AccountType == "Investment");
        investment.Percentage.Should().Be(80m);
    }

    [Fact]
    public async Task GetNetWorthSummary_UsesComputedBalancesForBankingCreditAndPropertyAccounts()
    {
        var userId = Guid.NewGuid();

        var banking = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(500m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        )
        {
            CurrentBalance = new Money(0m),
        };

        var credit = new Account(
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        )
        {
            CurrentBalance = new Money(0m),
        };

        var property = new Account(
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            "CAD",
            new Money(770_000m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            userId
        )
        {
            CurrentBalance = new Money(0m),
        };

        AddValuation(property, 850_000m, new DateOnly(2026, 4, 1));

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([banking, credit, property]);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal> { [banking.Id] = 250m, [credit.Id] = -1200m });

        var result = await CreateHandler()
            .Handle(new GetNetWorthSummaryQuery(userId), CancellationToken.None);

        result.TotalAssets.Should().Be(850_750m);
        result.TotalLiabilities.Should().Be(1200m);
        result.NetWorth.Should().Be(849_550m);

        result.AccountSummaries.Should().ContainSingle(a => a.Name == "Chequing" && a.Balance == 750m);
        result.AccountSummaries.Should().ContainSingle(a => a.Name == "Visa" && a.Balance == -1200m);
        result.AccountSummaries.Should().ContainSingle(a => a.Name == "House" && a.Balance == 850_000m);
    }

    [Fact]
    public async Task GetNetWorthSummary_UsesInvestmentMarketValueWhenCachedBalanceIsStale()
    {
        var userId = Guid.NewGuid();
        var investment = new Account(
            "Wealthsimple TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0.01m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        )
        {
            CurrentBalance = new Money(0m),
        };

        var security = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio"
        );
        var holding = SecurityTestHelper.CreateHolding(
            investment.Id,
            security,
            100m,
            new Money(35m, "CAD")
        );

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([investment]);

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(investment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _priceHistoryRepositoryMock
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory>
                    {
                        [security.Id] = SecurityTestHelper.CreatePriceHistory(
                            security,
                            40m,
                            DateOnly.FromDateTime(DateTime.UtcNow)
                        ),
                    }
            );

        var result = await CreateHandler()
            .Handle(new GetNetWorthSummaryQuery(userId), CancellationToken.None);

        result.TotalAssets.Should().Be(4000m);
        result.NetWorth.Should().Be(4000m);
        result.AccountSummaries.Should().ContainSingle(a => a.Name == "Wealthsimple TFSA" && a.Balance == 4000m);
    }

    private static Account CreateAccount(
        Guid ownerId,
        string name,
        AccountType accountType,
        AccountSubType accountSubType,
        decimal openingBalance,
        string currency = "CAD"
    ) =>
        new(
            name,
            accountType,
            accountSubType,
            currency,
            new Money(openingBalance, currency),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ownerId
        );

    private static void AddValuation(Account account, decimal estimatedValue, DateOnly effectiveDate)
    {
        var valuationsField = typeof(Account).GetField("_valuations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var valuations = valuationsField!.GetValue(account) as List<Valuation>;
        valuations!.Add(new Valuation(account.Id, new Money(estimatedValue, account.Currency), effectiveDate, "Test"));
    }
}
