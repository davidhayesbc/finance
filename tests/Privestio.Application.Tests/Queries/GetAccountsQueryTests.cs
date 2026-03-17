using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccounts;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IHoldingRepository> _holdingRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IPriceFeedProvider> _priceFeedProviderMock;
    private readonly SecurityResolutionService _securityResolutionService;

    public GetAccountsQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _holdingRepositoryMock = new Mock<IHoldingRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _priceFeedProviderMock = new Mock<IPriceFeedProvider>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Holdings).Returns(_holdingRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.PriceHistories).Returns(_priceHistoryRepositoryMock.Object);
        _securityResolutionService = SecurityTestHelper.CreateSecurityResolutionService(
            _unitOfWorkMock
        );
        _priceFeedProviderMock.SetupGet(x => x.ProviderName).Returns("YahooFinance");

        _priceFeedProviderMock
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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
    }

    private GetAccountsQueryHandler CreateHandler() =>
        new(_unitOfWorkMock.Object, _priceFeedProviderMock.Object, _securityResolutionService);

    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmptyList()
    {
        var ownerId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        var result = await CreateHandler()
            .Handle(new GetAccountsQuery(ownerId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MixedAccounts_ComputesCorrectBalances()
    {
        var ownerId = Guid.NewGuid();
        var banking = CreateAccount(
            ownerId,
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            500m
        );
        var credit = CreateAccount(
            ownerId,
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            0m
        );
        var property = CreateAccount(
            ownerId,
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            770_000m
        );
        AddValuation(property, 850_000m, new DateOnly(2026, 1, 1));

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { banking, credit, property });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new Dictionary<Guid, decimal> { { banking.Id, 250m }, { credit.Id, -1200m } }
            );

        var result = await CreateHandler()
            .Handle(new GetAccountsQuery(ownerId), CancellationToken.None);

        result.Should().HaveCount(3);

        var bankingResult = result.Single(a => a.Name == "Chequing");
        bankingResult.CurrentBalance.Should().Be(750m);

        var creditResult = result.Single(a => a.Name == "Visa");
        creditResult.CurrentBalance.Should().Be(-1200m);

        var propertyResult = result.Single(a => a.Name == "House");
        propertyResult.CurrentBalance.Should().Be(850_000m);
    }

    [Fact]
    public async Task Handle_PropertyAccountsExcludedFromTransactionQuery()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateAccount(
            ownerId,
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            770_000m
        );

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { property });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        var result = await CreateHandler()
            .Handle(new GetAccountsQuery(ownerId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CurrentBalance.Should().Be(770_000m);

        _transactionRepositoryMock.Verify(
            x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_InvestmentAccount_UsesHoldingsMarketValueForCurrentBalance()
    {
        var ownerId = Guid.NewGuid();
        var investment = CreateAccount(
            ownerId,
            "Wealthsimple TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            0.01m
        );
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

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { investment });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

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
            .Handle(new GetAccountsQuery(ownerId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CurrentBalance.Should().Be(4000m);
    }

    [Fact]
    public async Task Handle_InvestmentAccount_MissingStoredPrice_UsesLiveQuote()
    {
        var ownerId = Guid.NewGuid();
        var investment = CreateAccount(
            ownerId,
            "Wealthsimple TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            0.01m
        );
        var security = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio",
            aliases: [("XEQT.TO", "YahooFinance", true)]
        );
        var holding = SecurityTestHelper.CreateHolding(
            investment.Id,
            security,
            100m,
            new Money(35m, "CAD")
        );

        SecurityTestHelper.CreateSecurityResolutionService(_unitOfWorkMock, [security]);

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { investment });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

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
                (IReadOnlyDictionary<Guid, PriceHistory>)new Dictionary<Guid, PriceHistory>()
            );

        _priceFeedProviderMock
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.Is<IEnumerable<PriceLookup>>(lookups =>
                        lookups.Single().SecurityId == security.Id
                        && lookups.Single().Symbol == "XEQT.TO"
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([
                new PriceQuote(
                    security.Id,
                    "XEQT.TO",
                    40m,
                    "CAD",
                    DateOnly.FromDateTime(DateTime.UtcNow)
                ),
            ]);

        var result = await CreateHandler()
            .Handle(new GetAccountsQuery(ownerId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CurrentBalance.Should().Be(4000m);
    }

    private static Account CreateAccount(
        Guid ownerId,
        string name,
        AccountType type,
        AccountSubType subType,
        decimal openingBalance
    ) =>
        new(
            name,
            type,
            subType,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2025, 1, 1),
            ownerId
        );

    private static void AddValuation(Account account, decimal amount, DateOnly effectiveDate)
    {
        var valuationsField = typeof(Account).GetField(
            "_valuations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var valuations = (List<Valuation>)valuationsField.GetValue(account)!;
        valuations.Add(new Valuation(account.Id, new Money(amount), effectiveDate, "Appraisal"));
    }
}
