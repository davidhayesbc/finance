using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccountById;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountByIdQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IHoldingRepository> _holdingRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IPriceFeedProvider> _priceFeedProviderMock;
    private readonly Mock<IExchangeRateRepository> _exchangeRateRepositoryMock;
    private readonly Mock<IExchangeRateProvider> _exchangeRateProviderMock;
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public GetAccountByIdQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _holdingRepositoryMock = new Mock<IHoldingRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _priceFeedProviderMock = new Mock<IPriceFeedProvider>();
        _exchangeRateRepositoryMock = new Mock<IExchangeRateRepository>();
        _exchangeRateProviderMock = new Mock<IExchangeRateProvider>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Holdings).Returns(_holdingRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.PriceHistories).Returns(_priceHistoryRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.ExchangeRates).Returns(_exchangeRateRepositoryMock.Object);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

        _priceFeedProviderMock.SetupGet(x => x.ProviderName).Returns("YahooFinance");
        _exchangeRateProviderMock.SetupGet(x => x.ProviderName).Returns("Frankfurter");

        _priceFeedProviderMock
            .Setup(x =>
                x.GetLatestPricesAsync(
                    It.IsAny<IEnumerable<PriceLookup>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _exchangeRateProviderMock
            .Setup(x =>
                x.GetLatestRatesAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        _exchangeRateProviderMock
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

        _priceHistoryRepositoryMock
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(Guid SecurityId, DateOnly AsOfDate)>());

        _exchangeRateRepositoryMock
            .Setup(x =>
                x.GetLatestByPairAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((ExchangeRate?)null);

        _exchangeRateRepositoryMock
            .Setup(x =>
                x.GetAllAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AccountNotOwned_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 1000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(accountId, requestingUserId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BankingAccount_CurrentBalanceIsOpeningPlusTransactions()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 500m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x => x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(250m);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(750m);
    }

    [Fact]
    public async Task Handle_BankingAccount_NoTransactions_CurrentBalanceIsOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 1000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x => x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_PropertyAccount_CurrentBalanceIsLatestValuation()
    {
        var ownerId = Guid.NewGuid();
        var account = CreatePropertyAccount(ownerId, openingBalance: 770_000m);
        AddValuation(account, 800_000m, new DateOnly(2025, 6, 1));
        AddValuation(account, 850_000m, new DateOnly(2026, 1, 1));
        AddValuation(account, 820_000m, new DateOnly(2025, 9, 1));

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(850_000m);

        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_PropertyAccount_NoValuations_FallsBackToOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreatePropertyAccount(ownerId, openingBalance: 770_000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = CreateHandler();
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(770_000m);
    }

    [Fact]
    public async Task Handle_InvestmentAccount_CurrentBalanceIsStoredPriceMarketValue()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateInvestmentAccount(ownerId, openingBalance: 0.01m);
        var security = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio"
        );
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            100m,
            new Money(35m, "CAD")
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
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
            .Handle(new GetAccountByIdQuery(account.Id, ownerId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(4000m);

        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_InvestmentAccount_ConvertsQuoteCurrencyToAccountCurrency()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateInvestmentAccount(ownerId, openingBalance: 0.01m);
        var security = SecurityTestHelper.CreateSecurity("EEMV", "Emerging Markets ETF", "USD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            100m,
            new Money(12m, "CAD")
        );
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
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
                            10m,
                            asOfDate,
                            "Yahoo"
                        ),
                    }
            );

        _exchangeRateRepositoryMock
            .Setup(x => x.GetAllAsync("USD", "CAD", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExchangeRate("USD", "CAD", 1.25m, asOfDate, "Frankfurter")]);

        var result = await CreateHandler()
            .Handle(new GetAccountByIdQuery(account.Id, ownerId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(1250m);

        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private GetAccountByIdQueryHandler CreateHandler() =>
        new(_unitOfWorkMock.Object, new AccountBalanceService(_unitOfWorkMock.Object, _investmentPortfolioValuationService));

    private static Account CreateBankingAccount(Guid ownerId, decimal openingBalance) =>
        new(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2025, 1, 1),
            ownerId
        );

    private static Account CreatePropertyAccount(Guid ownerId, decimal openingBalance) =>
        new(
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2020, 6, 1),
            ownerId
        );

    private static Account CreateInvestmentAccount(Guid ownerId, decimal openingBalance) =>
        new(
            "Wealthsimple TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
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
