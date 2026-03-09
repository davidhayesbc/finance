using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetNetWorthSummary;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class NetWorthSummaryQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;

    public NetWorthSummaryQueryTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
    }

    [Fact]
    public async Task GetNetWorthSummary_WithMixedAccounts_CalculatesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chequing = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        chequing.CurrentBalance = new Money(5000m);

        var rrsp = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        rrsp.CurrentBalance = new Money(50000m);

        var creditCard = new Account(
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        creditCard.CurrentBalance = new Money(-2000m);

        var mortgage = new Account(
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-5),
            userId
        );
        mortgage.CurrentBalance = new Money(-300000m);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { chequing, rrsp, creditCard, mortgage });

        var handler = new GetNetWorthSummaryQueryHandler(_unitOfWorkMock.Object);
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

        var handler = new GetNetWorthSummaryQueryHandler(_unitOfWorkMock.Object);
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

        var chequing = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        chequing.CurrentBalance = new Money(20000m);

        var rrsp = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );
        rrsp.CurrentBalance = new Money(80000m);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { chequing, rrsp });

        var handler = new GetNetWorthSummaryQueryHandler(_unitOfWorkMock.Object);
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
}
