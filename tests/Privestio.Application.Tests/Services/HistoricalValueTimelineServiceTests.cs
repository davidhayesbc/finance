using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class HistoricalValueTimelineServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<IHoldingRepository> _holdings = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistories = new();
    private readonly Mock<IExchangeRateRepository> _exchangeRates = new();
    private readonly Mock<IExchangeRateProvider> _exchangeRateProvider = new();

    public HistoricalValueTimelineServiceTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.Transactions).Returns(_transactions.Object);
        _unitOfWork.SetupGet(x => x.Holdings).Returns(_holdings.Object);
        _unitOfWork.SetupGet(x => x.PriceHistories).Returns(_priceHistories.Object);
        _unitOfWork.SetupGet(x => x.ExchangeRates).Returns(_exchangeRates.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _exchangeRateProvider.SetupGet(x => x.ProviderName).Returns("Frankfurter");
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
                x.GetAllAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task GetNetWorthHistoryAsync_TransactionalAccounts_ComputesDailyNetWorth()
    {
        var ownerId = Guid.NewGuid();
        var chequing = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(1000m, "CAD"),
            new DateOnly(2024, 1, 1),
            ownerId
        );
        var visa = new Account(
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            "CAD",
            new Money(0m, "CAD"),
            new DateOnly(2024, 1, 1),
            ownerId
        );

        var payrollDebit = new Transaction(
            chequing.Id,
            new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            new Money(100m, "CAD"),
            "Groceries",
            TransactionType.Debit
        );
        var cardCharge = new Transaction(
            visa.Id,
            new DateTime(2024, 1, 3, 12, 0, 0, DateTimeKind.Utc),
            new Money(200m, "CAD"),
            "Flight",
            TransactionType.Debit
        );

        _accounts
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chequing, visa]);
        _transactions
            .Setup(x =>
                x.GetByOwnerAndDateRangeAsync(
                    ownerId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([payrollDebit, cardCharge]);

        var service = new HistoricalValueTimelineService(
            _unitOfWork.Object,
            _exchangeRateProvider.Object
        );

        var result = await service.GetNetWorthHistoryAsync(
            ownerId,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 3),
            CancellationToken.None
        );

        result.Should().HaveCount(3);
        result[0].Value.Should().Be(1000m);
        result[1].Value.Should().Be(900m);
        result[2].Value.Should().Be(700m);
    }

    [Fact]
    public async Task GetAccountHistoryAsync_InvestmentAccount_UsesLotsAndPriceHistory()
    {
        var ownerId = Guid.NewGuid();
        var account = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m, "CAD"),
            new DateOnly(2024, 1, 1),
            ownerId
        );
        var security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard S&P 500", "CAD");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            15m,
            new Money(100m, "CAD")
        );

        var firstLot = new Lot(holding.Id, new DateOnly(2024, 1, 1), 10m, new Money(90m, "CAD"));
        var secondLot = new Lot(holding.Id, new DateOnly(2024, 1, 3), 5m, new Money(95m, "CAD"));
        var lots =
            (List<Lot>)
                typeof(Holding)
                    .GetField(
                        "_lots",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )!
                    .GetValue(holding)!;
        lots.Add(firstLot);
        lots.Add(secondLot);
        holding.RebindSecurity(security);

        var prices = new List<PriceHistory>
        {
            SecurityTestHelper.CreatePriceHistory(security, 100m, new DateOnly(2024, 1, 1)),
            SecurityTestHelper.CreatePriceHistory(security, 110m, new DateOnly(2024, 1, 2)),
            SecurityTestHelper.CreatePriceHistory(security, 120m, new DateOnly(2024, 1, 3)),
        };

        _holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        _priceHistories
            .Setup(x =>
                x.GetBySecurityIdAsync(
                    security.Id,
                    null,
                    new DateOnly(2024, 1, 3),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(prices);

        var service = new HistoricalValueTimelineService(
            _unitOfWork.Object,
            _exchangeRateProvider.Object
        );

        var result = await service.GetAccountHistoryAsync(
            account,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 3),
            CancellationToken.None
        );

        result.Select(point => point.Value).Should().Equal(1000m, 1100m, 1800m);
    }
}
