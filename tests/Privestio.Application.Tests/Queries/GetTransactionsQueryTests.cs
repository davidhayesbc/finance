using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetTransactions;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetTransactionsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;

    public GetTransactionsQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_OwnedAccount_ForwardsSearchTermAndReturnsPagedTransactions()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transaction = new Transaction(
            accountId,
            new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            new Money(42.50m),
            "Coffee subscription",
            TransactionType.Debit
        );

        var account = new Account(
            "Main chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(100m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            ownerId
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x => x.GetPagedAsync(
                accountId,
                15,
                null,
                It.IsAny<DateRange?>(),
                null,
                "coffee",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction> { transaction }, "next-cursor"));

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(
            accountId,
            ownerId,
            15,
            null,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            null,
            "coffee"
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Description.Should().Be("Coffee subscription");
        result.NextCursor.Should().Be("next-cursor");
    }

    [Fact]
    public async Task Handle_AccountNotOwned_ReturnsEmptyPage()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account(
            "Brokerage",
            AccountType.Investment,
            AccountSubType.NonRegistered,
            "CAD",
            new Money(1000m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)),
            otherOwnerId
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(accountId, ownerId, 20, SearchTerm: "rent");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.PageSize.Should().Be(20);
        _transactionRepositoryMock.Verify(
            x => x.GetPagedAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateRange?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}