using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetDebtOverview;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class DebtOverviewQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IAmortizationEntryRepository> _amortizationRepoMock;

    public DebtOverviewQueryTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _amortizationRepoMock = new Mock<IAmortizationEntryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AmortizationEntries).Returns(_amortizationRepoMock.Object);
    }

    [Fact]
    public async Task GetDebtOverview_WithLoans_ReturnsTotalDebt()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var mortgage = new Account(
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5)),
            userId
        );
        mortgage.CurrentBalance = new Money(-250000m);

        var creditCard = new Account(
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            userId
        );
        creditCard.CurrentBalance = new Money(-3000m);

        var mortgageEntries = new List<AmortizationEntry>
        {
            new(
                mortgage.Id,
                1,
                new DateOnly(2025, 1, 1),
                new Money(1500m, "CAD"),
                new Money(500m, "CAD"),
                new Money(1000m, "CAD"),
                new Money(249500m, "CAD")
            ),
            new(
                mortgage.Id,
                2,
                new DateOnly(2025, 2, 1),
                new Money(1500m, "CAD"),
                new Money(502m, "CAD"),
                new Money(998m, "CAD"),
                new Money(248998m, "CAD")
            ),
        };

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { mortgage, creditCard });

        _amortizationRepoMock
            .Setup(r => r.GetByAccountIdAsync(mortgage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mortgageEntries);

        _amortizationRepoMock
            .Setup(r => r.GetByAccountIdAsync(creditCard.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AmortizationEntry>());

        var handler = new GetDebtOverviewQueryHandler(_unitOfWorkMock.Object);
        var query = new GetDebtOverviewQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalDebt.Should().Be(253000m); // abs(-250000) + abs(-3000)
        result.Currency.Should().Be("CAD");
        result.Debts.Should().HaveCount(2);

        var mortgageDebt = result.Debts.First(d => d.Name == "Mortgage");
        mortgageDebt.Balance.Should().Be(250000m);
        mortgageDebt.MonthlyPayment.Should().Be(1500m);
    }

    [Fact]
    public async Task GetDebtOverview_NoDebts_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chequing = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        );
        chequing.CurrentBalance = new Money(5000m);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { chequing });

        var handler = new GetDebtOverviewQueryHandler(_unitOfWorkMock.Object);
        var query = new GetDebtOverviewQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalDebt.Should().Be(0m);
        result.Debts.Should().BeEmpty();
    }
}
