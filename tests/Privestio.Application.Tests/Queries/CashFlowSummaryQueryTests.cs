using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetCashFlowSummary;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class CashFlowSummaryQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;

    public CashFlowSummaryQueryTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
    }

    [Fact]
    public async Task GetCashFlowSummary_WithIncomeAndExpenses_CalculatesSavingsRate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var income = new Transaction(
            accountId,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new Money(5000m),
            "Salary",
            TransactionType.Credit
        );

        var expense1 = new Transaction(
            accountId,
            new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            new Money(1500m),
            "Rent",
            TransactionType.Debit
        );

        var expense2 = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(500m),
            "Groceries",
            TransactionType.Debit
        );

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { income, expense1, expense2 });

        var handler = new GetCashFlowSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetCashFlowSummaryQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalIncome.Should().Be(5000m);
        result.TotalExpenses.Should().Be(2000m);
        result.NetSavings.Should().Be(3000m);
        result.SavingsRate.Should().Be(60m); // 3000/5000 * 100
        result.Currency.Should().Be("CAD");
    }

    [Fact]
    public async Task GetCashFlowSummary_ExcludesTransfers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var income = new Transaction(
            accountId,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new Money(3000m),
            "Salary",
            TransactionType.Credit
        );

        var transfer = new Transaction(
            accountId,
            new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            new Money(1000m),
            "Transfer to Savings",
            TransactionType.Transfer
        );

        var expense = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(500m),
            "Groceries",
            TransactionType.Debit
        );

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { income, transfer, expense });

        var handler = new GetCashFlowSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetCashFlowSummaryQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Transfer should be excluded from both income and expenses
        result.TotalIncome.Should().Be(3000m);
        result.TotalExpenses.Should().Be(500m);
        result.NetSavings.Should().Be(2500m);
    }

    [Fact]
    public async Task GetCashFlowSummary_ZeroIncome_ZeroSavingsRate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var expense = new Transaction(
            accountId,
            new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            new Money(500m),
            "Groceries",
            TransactionType.Debit
        );

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { expense });

        var handler = new GetCashFlowSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetCashFlowSummaryQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - When income is zero, savings rate should be 0 (not divide by zero)
        result.TotalIncome.Should().Be(0m);
        result.TotalExpenses.Should().Be(500m);
        result.SavingsRate.Should().Be(0m);
    }
}
