using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class NetWorthForecastingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IForecastScenarioRepository> _forecastRepoMock;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepoMock;
    private readonly NetWorthForecastingService _service;

    public NetWorthForecastingServiceTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _forecastRepoMock = new Mock<IForecastScenarioRepository>();
        _recurringRepoMock = new Mock<IRecurringTransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ForecastScenarios).Returns(_forecastRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.RecurringTransactions).Returns(_recurringRepoMock.Object);

        _service = new NetWorthForecastingService();
    }

    [Fact]
    public async Task ProjectNetWorth_WithAccounts_ReturnsProjection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        var account = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(10000m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        account.CurrentBalance = new Money(10000m);

        var scenario = new ForecastScenario(userId, "Base Scenario");

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        _recurringRepoMock
            .Setup(r => r.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        // Act
        var result = await _service.ProjectNetWorth(
            _unitOfWorkMock.Object,
            userId,
            scenarioId,
            12,
            CancellationToken.None
        );

        // Assert
        result.Should().NotBeNull();
        result.Periods.Should().HaveCount(12);
        result.ScenarioName.Should().Be("Base Scenario");
        result.Currency.Should().Be("CAD");
        result
            .Periods.Should()
            .AllSatisfy(p =>
            {
                p.ProjectedAssets.Should().BeGreaterThanOrEqualTo(0);
            });
    }

    [Fact]
    public async Task ProjectNetWorth_AppliesGrowthRates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        var account = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(100000m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        account.CurrentBalance = new Money(100000m);

        var scenario = new ForecastScenario(userId, "Growth Scenario");
        // Add growth assumption for Investment type: 6% growth, 2% inflation = 4% real
        var assumptions = new List<GrowthAssumption>
        {
            new(null, AccountType.Investment, 6.0m, 2.0m),
        };
        scenario.UpdateAssumptions(assumptions);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        _recurringRepoMock
            .Setup(r => r.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        // Act
        var result = await _service.ProjectNetWorth(
            _unitOfWorkMock.Object,
            userId,
            scenarioId,
            12,
            CancellationToken.None
        );

        // Assert - After 12 months of 4% real growth, balance should be higher
        var lastPeriod = result.Periods.Last();
        lastPeriod.ProjectedAssets.Should().BeGreaterThan(100000m);
    }

    [Fact]
    public async Task ProjectNetWorth_IncludesRecurringTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();

        var account = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(5000m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        account.CurrentBalance = new Money(5000m);

        var scenario = new ForecastScenario(userId, "With Recurring");

        var recurringIncome = new RecurringTransaction(
            userId,
            account.Id,
            "Salary",
            new Money(3000m),
            TransactionType.Credit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        _recurringRepoMock
            .Setup(r => r.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringIncome });

        // Act
        var result = await _service.ProjectNetWorth(
            _unitOfWorkMock.Object,
            userId,
            scenarioId,
            6,
            CancellationToken.None
        );

        // Assert - After 6 months of $3000 monthly income, assets should grow significantly
        var lastPeriod = result.Periods.Last();
        lastPeriod.ProjectedAssets.Should().BeGreaterThan(5000m + 3000m * 5);
    }

    [Fact]
    public async Task ProjectNetWorth_ScenarioNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario?)null);

        // Act & Assert
        await _service
            .Invoking(s =>
                s.ProjectNetWorth(
                    _unitOfWorkMock.Object,
                    userId,
                    Guid.NewGuid(),
                    12,
                    CancellationToken.None
                )
            )
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }
}
