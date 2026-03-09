using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetSpendingAnalysis;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class SpendingAnalysisQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;

    public SpendingAnalysisQueryTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
    }

    [Fact]
    public async Task GetSpendingAnalysis_WithTransactions_ReturnsCategoryBreakdown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var groceryCategoryId = Guid.NewGuid();
        var gasCategoryId = Guid.NewGuid();

        var groceryCategory = new Category("Groceries", CategoryType.Expense);
        var gasCategory = new Category("Gas", CategoryType.Expense);

        var txn1 = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(150m),
            "Grocery Store",
            TransactionType.Debit
        )
        {
            CategoryId = groceryCategoryId,
            Category = groceryCategory,
        };

        var txn2 = new Transaction(
            accountId,
            new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new Money(50m),
            "Gas Station",
            TransactionType.Debit
        )
        {
            CategoryId = gasCategoryId,
            Category = gasCategory,
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
            .ReturnsAsync(new List<Transaction> { txn1, txn2 });

        var handler = new GetSpendingAnalysisQueryHandler(_unitOfWorkMock.Object);
        var query = new GetSpendingAnalysisQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalSpent.Should().Be(200m);
        result.CategoryBreakdown.Should().HaveCount(2);
        result.CategoryBreakdown.First().CategoryName.Should().Be("Groceries");
        result.CategoryBreakdown.First().Amount.Should().Be(150m);
        result.CategoryBreakdown.First().Percentage.Should().Be(75m);
    }

    [Fact]
    public async Task GetSpendingAnalysis_SplitTransactions_UseSplitCategories()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var groceryCategoryId = Guid.NewGuid();
        var householdCategoryId = Guid.NewGuid();

        var groceryCategory = new Category("Groceries", CategoryType.Expense);
        var householdCategory = new Category("Household", CategoryType.Expense);

        var splitTxn = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(200m),
            "Costco",
            TransactionType.Debit
        );

        // Add splits to the transaction
        var split1 = new TransactionSplit(splitTxn.Id, new Money(150m), groceryCategoryId)
        {
            Category = groceryCategory,
        };

        var split2 = new TransactionSplit(splitTxn.Id, new Money(50m), householdCategoryId)
        {
            Category = householdCategory,
        };

        splitTxn.AddSplit(split1);
        splitTxn.AddSplit(split2);

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

        var handler = new GetSpendingAnalysisQueryHandler(_unitOfWorkMock.Object);
        var query = new GetSpendingAnalysisQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Split categories should be used, not the parent transaction's category
        result.CategoryBreakdown.Should().HaveCount(2);
        var groceryBreakdown = result.CategoryBreakdown.First(c => c.CategoryName == "Groceries");
        groceryBreakdown.Amount.Should().Be(150m);
        var householdBreakdown = result.CategoryBreakdown.First(c => c.CategoryName == "Household");
        householdBreakdown.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task GetSpendingAnalysis_PayeeRanking_OrdersByAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var payeeA = new Payee("Store A", userId);
        var payeeB = new Payee("Store B", userId);

        var txn1 = new Transaction(
            accountId,
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new Money(100m),
            "Purchase A1",
            TransactionType.Debit
        )
        {
            CategoryId = categoryId,
            Payee = payeeA,
        };

        var txn2 = new Transaction(
            accountId,
            new DateTime(2025, 6, 12, 0, 0, 0, DateTimeKind.Utc),
            new Money(200m),
            "Purchase B1",
            TransactionType.Debit
        )
        {
            CategoryId = categoryId,
            Payee = payeeB,
        };

        var txn3 = new Transaction(
            accountId,
            new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            new Money(50m),
            "Purchase A2",
            TransactionType.Debit
        )
        {
            CategoryId = categoryId,
            Payee = payeeA,
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
            .ReturnsAsync(new List<Transaction> { txn1, txn2, txn3 });

        var handler = new GetSpendingAnalysisQueryHandler(_unitOfWorkMock.Object);
        var query = new GetSpendingAnalysisQuery(
            userId,
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Store B ($200) should be ranked first, then Store A ($150)
        result.PayeeRanking.Should().HaveCount(2);
        result.PayeeRanking[0].PayeeName.Should().Be("Store B");
        result.PayeeRanking[0].Amount.Should().Be(200m);
        result.PayeeRanking[0].TransactionCount.Should().Be(1);
        result.PayeeRanking[1].PayeeName.Should().Be("Store A");
        result.PayeeRanking[1].Amount.Should().Be(150m);
        result.PayeeRanking[1].TransactionCount.Should().Be(2);
    }
}
