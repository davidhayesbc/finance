using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Services;

/// <summary>
/// Verifies that AccountBalanceService is the single authoritative code path for
/// account balance derivation. These tests guard against the divergence bug where
/// GetAccountById, GetAccounts, and GetNetWorthSummary returned different balances
/// for the same account because each handler had its own private ComputeBalance copy.
///
/// Balance derivation rules under test:
///   Property   — latest Valuation by EffectiveDate; fallback to OpeningBalance
///   Investment — InvestmentPortfolioValuationService.TotalMarketValue; fallback to CurrentBalance
///   Banking    — OpeningBalance + signed transaction sum
///   Credit     — OpeningBalance + signed transaction sum
///   Loan       — OpeningBalance + signed transaction sum
/// </summary>
public class AccountBalanceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IHoldingRepository> _holdingRepositoryMock;
    private readonly Mock<IHoldingSnapshotRepository> _holdingSnapshotRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IExchangeRateRepository> _exchangeRateRepositoryMock;
    private readonly Mock<IPriceFeedProvider> _priceFeedProviderMock;
    private readonly Mock<IExchangeRateProvider> _exchangeRateProviderMock;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;
    private readonly AccountBalanceService _sut;

    public AccountBalanceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _holdingRepositoryMock = new Mock<IHoldingRepository>();
        _holdingSnapshotRepositoryMock = new Mock<IHoldingSnapshotRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _exchangeRateRepositoryMock = new Mock<IExchangeRateRepository>();
        _priceFeedProviderMock = new Mock<IPriceFeedProvider>();
        _exchangeRateProviderMock = new Mock<IExchangeRateProvider>();

        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Holdings).Returns(_holdingRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.HoldingSnapshots).Returns(_holdingSnapshotRepositoryMock.Object);
        _unitOfWorkMock
            .SetupGet(x => x.PriceHistories)
            .Returns(_priceHistoryRepositoryMock.Object);
        _unitOfWorkMock
            .SetupGet(x => x.ExchangeRates)
            .Returns(_exchangeRateRepositoryMock.Object);

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

        // Default: no snapshots — existing investment tests use portfolio valuation path.
        _holdingSnapshotRepositoryMock
            .Setup(x =>
                x.GetCurrentSnapshotTotalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((decimal?)null);

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

        var securityResolutionService = SecurityTestHelper.CreateSecurityResolutionService(
            _unitOfWorkMock
        );

        _investmentPortfolioValuationService = new InvestmentPortfolioValuationService(
            _unitOfWorkMock.Object,
            _priceFeedProviderMock.Object,
            _exchangeRateProviderMock.Object,
            securityResolutionService,
            Options.Create(new PricingOptions())
        );

        _sut = new AccountBalanceService(
            _unitOfWorkMock.Object,
            _investmentPortfolioValuationService
        );
    }

    // ── Property accounts ────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_PropertyAccount_ReturnsLatestValuationByEffectiveDate()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 500_000m
        );
        AddValuation(account, 600_000m, new DateOnly(2025, 1, 1));
        AddValuation(account, 650_000m, new DateOnly(2026, 1, 1)); // latest by EffectiveDate
        AddValuation(account, 620_000m, new DateOnly(2025, 7, 1));

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(650_000m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_PropertyAccount_IgnoresDeletedValuations()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 500_000m
        );
        AddValuation(account, 800_000m, new DateOnly(2026, 1, 1), deleted: true); // deleted — must not be used
        AddValuation(account, 600_000m, new DateOnly(2025, 6, 1));

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(600_000m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_PropertyAccount_NoValuations_FallsBackToOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 500_000m
        );

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(500_000m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_PropertyAccount_TransactionsDoNotAffectBalance()
    {
        // Guard: property balance is valuation-based, NOT opening + transactions.
        // This is the core bug scenario: banking formula applied to property account.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 500_000m
        );
        AddValuation(account, 550_000m, new DateOnly(2025, 6, 1));

        // Even if someone calls GetSignedSumByAccountIdAsync, the result must not be used.
        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(-10_000m); // e.g. $10k in property tax expenses

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(550_000m); // valuation wins, not 490_000m
        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    // ── Batch overload — property ─────────────────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_BatchOverload_PropertyAccount_ReturnsLatestValuation()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 400_000m
        );
        AddValuation(account, 425_000m, new DateOnly(2026, 3, 1));

        var precomputedSums = new Dictionary<Guid, decimal>();

        var balance = await _sut.ComputeCurrentBalanceAsync(
            account,
            precomputedSums,
            CancellationToken.None
        );

        balance.Should().Be(425_000m);
    }

    // ── Investment accounts ───────────────────────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_NoHoldings_ReturnsZeroMarketValue()
    {
        // When there are no active holdings the portfolio is worth $0.
        // TotalMarketValue is 0m (not null), so the CurrentBalance fallback does NOT apply.
        // Null is only returned when holdings exist but prices are unavailable.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.TFSA,
            openingBalance: 10_000m
        );
        SetStoredCurrentBalance(account, 12_000m);

        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_WithHoldingsAndPrice_ReturnsMarketValue()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.TFSA,
            openingBalance: 0m
        );

        var security = SecurityTestHelper.CreateSecurity("AAPL", "Apple Inc.", "CAD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            quantity: 10m,
            averageCostPerUnit: new Money(100m)
        );
        var priceHistory = SecurityTestHelper.CreatePriceHistory(
            security,
            150m,
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

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
                    new Dictionary<Guid, PriceHistory> { [security.Id] = priceHistory }
            );

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(1_500m); // 10 × $150
    }

    // ── Investment accounts — snapshot-based ──────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_WithSnapshots_ReturnsSnapshotTotal()
    {
        // Sun Life RRSP scenario: managed fund imported from PDF; value is in HoldingSnapshots,
        // not in Holdings. The snapshot total must be used as the authoritative balance.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.RRSP,
            openingBalance: 0m
        );

        _holdingSnapshotRepositoryMock
            .Setup(x =>
                x.GetCurrentSnapshotTotalAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(1_001_625m);

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(1_001_625m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_WithSnapshots_PortfolioValuationNotCalled()
    {
        // When snapshots are present the portfolio valuation service (and Holdings repo) must
        // not be consulted — snapshots are the authoritative source.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.RRSP,
            openingBalance: 0m
        );

        _holdingSnapshotRepositoryMock
            .Setup(x =>
                x.GetCurrentSnapshotTotalAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(900_000m);

        await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        _holdingRepositoryMock.Verify(
            x => x.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_NoSnapshots_FallsThroughToPortfolioValuation()
    {
        // When no snapshots exist the service must fall through to portfolio valuation.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.TFSA,
            openingBalance: 0m
        );

        var security = SecurityTestHelper.CreateSecurity("VFV", "Vanguard S&P 500 ETF", "CAD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            quantity: 20m,
            averageCostPerUnit: new Money(100m)
        );
        var priceHistory = SecurityTestHelper.CreatePriceHistory(
            security,
            120m,
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // No snapshots — returns null → portfolio valuation path
        _holdingSnapshotRepositoryMock
            .Setup(x =>
                x.GetCurrentSnapshotTotalAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((decimal?)null);

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
                    new Dictionary<Guid, PriceHistory> { [security.Id] = priceHistory }
            );

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(2_400m); // 20 × $120
    }

    [Fact]
    public async Task ComputeCurrentBalance_InvestmentAccount_SnapshotTakesPriorityOverHoldings()
    {
        // Guard: even when Holdings exist, if snapshots also exist the snapshot total wins.
        // This ensures managed-fund accounts (Sun Life) always report snapshot-based value.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Investment,
            AccountSubType.RRSP,
            openingBalance: 0m
        );

        _holdingSnapshotRepositoryMock
            .Setup(x =>
                x.GetCurrentSnapshotTotalAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(1_001_625m); // snapshot says $1M+

        // Holdings also exist but must NOT be consulted.
        var security = SecurityTestHelper.CreateSecurity("SLGF", "Sun Life Growth Fund", "CAD");
        _holdingRepositoryMock
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    SecurityTestHelper.CreateHolding(
                        account.Id,
                        security,
                        quantity: 1000m,
                        averageCostPerUnit: new Money(685m)
                    ),
                ]
            );

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(1_001_625m); // snapshot wins, not $685,000
    }

    // ── Banking accounts ──────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_BankingAccount_ReturnsOpeningPlusSignedSum()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Banking,
            AccountSubType.Chequing,
            openingBalance: 1_000m
        );

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(2_500m);

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(3_500m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_BankingAccount_NoTransactions_ReturnsOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Banking,
            AccountSubType.Chequing,
            openingBalance: 5_000m
        );

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(0m);

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(5_000m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_CreditAccount_ReturnsOpeningPlusSignedSum()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Credit,
            AccountSubType.CreditCard,
            openingBalance: 0m
        );

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(-1_200m);

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(-1_200m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_LoanAccount_ReturnsOpeningPlusSignedSum()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Loan,
            AccountSubType.Mortgage,
            openingBalance: -350_000m
        );

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(50_000m); // principal payments received

        var balance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);

        balance.Should().Be(-300_000m);
    }

    // ── Batch overload — banking/credit/loan ──────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_BatchOverload_BankingAccount_UsesPrecomputedSum()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Banking,
            AccountSubType.Chequing,
            openingBalance: 1_000m
        );

        var precomputedSums = new Dictionary<Guid, decimal> { [account.Id] = 2_500m };

        var balance = await _sut.ComputeCurrentBalanceAsync(
            account,
            precomputedSums,
            CancellationToken.None
        );

        balance.Should().Be(3_500m);

        // Must NOT query the database again for the signed sum
        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ComputeCurrentBalance_BatchOverload_BankingAccount_MissingFromDictionary_TreatsAsZero()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Banking,
            AccountSubType.Chequing,
            openingBalance: 7_500m
        );

        var precomputedSums = new Dictionary<Guid, decimal>(); // empty — account not in dict

        var balance = await _sut.ComputeCurrentBalanceAsync(
            account,
            precomputedSums,
            CancellationToken.None
        );

        balance.Should().Be(7_500m); // OpeningBalance + 0
    }

    // ── Consistency guarantee ─────────────────────────────────────────────────

    [Fact]
    public async Task ComputeCurrentBalance_SingleAndBatchOverloads_ReturnSameResult_ForPropertyAccount()
    {
        // This test is the regression guard against the original bug:
        // GetAccountById (single overload) and GetAccounts (batch overload) must agree.
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Property,
            AccountSubType.RealEstate,
            openingBalance: 300_000m
        );
        AddValuation(account, 375_000m, new DateOnly(2025, 9, 1));

        var singleBalance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);
        var batchBalance = await _sut.ComputeCurrentBalanceAsync(
            account,
            new Dictionary<Guid, decimal>(),
            CancellationToken.None
        );

        singleBalance.Should().Be(batchBalance);
        singleBalance.Should().Be(375_000m);
    }

    [Fact]
    public async Task ComputeCurrentBalance_SingleAndBatchOverloads_ReturnSameResult_ForBankingAccount()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateAccount(
            ownerId,
            AccountType.Banking,
            AccountSubType.Chequing,
            openingBalance: 2_000m
        );

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(800m);

        var singleBalance = await _sut.ComputeCurrentBalanceAsync(account, CancellationToken.None);
        var batchBalance = await _sut.ComputeCurrentBalanceAsync(
            account,
            new Dictionary<Guid, decimal> { [account.Id] = 800m },
            CancellationToken.None
        );

        singleBalance.Should().Be(batchBalance);
        singleBalance.Should().Be(2_800m);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Account CreateAccount(
        Guid ownerId,
        AccountType accountType,
        AccountSubType accountSubType,
        decimal openingBalance
    ) =>
        new(
            "Test Account",
            accountType,
            accountSubType,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2020, 1, 1),
            ownerId
        );

    private static void AddValuation(
        Account account,
        decimal amount,
        DateOnly effectiveDate,
        bool deleted = false
    )
    {
        var valuationsField = typeof(Account).GetField(
            "_valuations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var valuations = (List<Valuation>)valuationsField.GetValue(account)!;
        var valuation = new Valuation(
            account.Id,
            new Money(amount),
            effectiveDate,
            "Appraisal"
        );
        if (deleted)
        {
            var isDeletedProp = typeof(Valuation)
                .BaseType!.GetProperty(
                    "IsDeleted",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                )!;
            isDeletedProp.SetValue(valuation, true);
        }

        valuations.Add(valuation);
    }

    private static void SetStoredCurrentBalance(Account account, decimal amount)
    {
        account.CurrentBalance = new Money(amount);
    }
}
