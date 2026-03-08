using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetBudgets;
using Privestio.Application.Queries.GetBudgetSummary;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class BudgetQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBudgetRepository> _budgetRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;

    public BudgetQueryTests()
    {
        _budgetRepoMock = new Mock<IBudgetRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Budgets).Returns(_budgetRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
    }

    // ── GetBudgets ──

    [Fact]
    public async Task GetBudgets_ReturnsBudgetsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = new Category("Groceries", CategoryType.Expense, userId);

        var budget = new Budget(userId, categoryId, 2025, 6, new Money(500m), false, "Test");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        var handler = new GetBudgetsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(500m);
        result[0].Year.Should().Be(2025);
        result[0].Month.Should().Be(6);
    }

    [Fact]
    public async Task GetBudgets_WithPeriodFilter_UsesFilteredQuery()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget>());

        var handler = new GetBudgetsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetsQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(
            r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // ── GetBudgetSummary ──

    [Fact]
    public async Task GetBudgetSummary_NoBudgets_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget>());

        var handler = new GetBudgetSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetSummaryQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBudgetSummary_WithTransactions_CalculatesActualSpending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var category = new Category("Groceries", CategoryType.Expense, userId);
        var budget = new Budget(userId, categoryId, 2025, 6, new Money(500m));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        var transaction = new Transaction(
            accountId,
            new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new Money(200m),
            "Grocery shopping",
            TransactionType.Debit
        )
        {
            CategoryId = categoryId,
        };

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { transaction });

        var handler = new GetBudgetSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetSummaryQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var summary = result[0];
        summary.BudgetedAmount.Should().Be(500m);
        summary.ActualAmount.Should().Be(200m);
        summary.RemainingAmount.Should().Be(300m);
        summary.PercentageUsed.Should().Be(40m);
        summary.IsOverBudget.Should().BeFalse();
    }

    [Fact]
    public async Task GetBudgetSummary_OverBudget_FlagsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var budget = new Budget(userId, categoryId, 2025, 6, new Money(100m));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        var transaction = new Transaction(
            accountId,
            new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new Money(150m),
            "Over spending",
            TransactionType.Debit
        )
        {
            CategoryId = categoryId,
        };

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { transaction });

        var handler = new GetBudgetSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetSummaryQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var summary = result[0];
        summary.IsOverBudget.Should().BeTrue();
        summary.RemainingAmount.Should().Be(-50m);
        summary.PercentageUsed.Should().Be(150m);
    }

    [Fact]
    public async Task GetBudgetSummary_SplitTransaction_UsesSplitCategories()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groceriesCategoryId = Guid.NewGuid();
        var diningCategoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var groceryBudget = new Budget(userId, groceriesCategoryId, 2025, 6, new Money(400m));
        var diningBudget = new Budget(userId, diningCategoryId, 2025, 6, new Money(200m));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { groceryBudget, diningBudget });

        // Create a split transaction: $300 total, $200 groceries + $100 dining
        var splitTxn = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(300m),
            "Costco mixed purchase",
            TransactionType.Debit
        );
        splitTxn.AddSplit(new TransactionSplit(splitTxn.Id, new Money(200m), groceriesCategoryId));
        splitTxn.AddSplit(new TransactionSplit(splitTxn.Id, new Money(100m), diningCategoryId));

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { splitTxn });

        var handler = new GetBudgetSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetSummaryQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        var grocerySummary = result.First(s => s.CategoryId == groceriesCategoryId);
        grocerySummary.ActualAmount.Should().Be(200m);
        grocerySummary.RemainingAmount.Should().Be(200m);

        var diningSummary = result.First(s => s.CategoryId == diningCategoryId);
        diningSummary.ActualAmount.Should().Be(100m);
        diningSummary.RemainingAmount.Should().Be(100m);
    }

    [Fact]
    public async Task GetBudgetSummary_TransfersIgnored()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var budget = new Budget(userId, categoryId, 2025, 6, new Money(500m));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAndPeriodAsync(userId, 2025, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        var transfer = new Transaction(
            accountId,
            new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new Money(500m),
            "Transfer to savings",
            TransactionType.Transfer
        )
        {
            CategoryId = categoryId,
        };

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { transfer });

        var handler = new GetBudgetSummaryQueryHandler(_unitOfWorkMock.Object);
        var query = new GetBudgetSummaryQuery(userId, 2025, 6);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Transfer should not count as spending
        result[0].ActualAmount.Should().Be(0m);
        result[0].IsOverBudget.Should().BeFalse();
    }
}
