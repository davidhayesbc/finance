using FluentAssertions;
using Moq;
using Privestio.Application.Commands.UpdateTransactionSplits;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class UpdateTransactionSplitsCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly UpdateTransactionSplitsCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    public UpdateTransactionSplitsCommandTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transactionRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        _handler = new UpdateTransactionSplitsCommandHandler(_unitOfWorkMock.Object);
    }

    private Transaction CreateTestTransaction(decimal amount = 100.00m)
    {
        return new Transaction(
            _accountId,
            DateTime.UtcNow,
            new Money(amount, "CAD"),
            "Test Transaction",
            TransactionType.Debit
        );
    }

    private Account CreateTestAccount()
    {
        return new Account(
            name: "Test Account",
            accountType: AccountType.Banking,
            accountSubType: AccountSubType.Chequing,
            currency: "CAD",
            openingBalance: new Money(0, "CAD"),
            openingDate: DateTime.UtcNow.AddYears(-1),
            ownerId: _userId
        );
    }

    [Fact]
    public async Task Handle_ValidSplits_ReturnsSplitResponses()
    {
        // Arrange
        var transaction = CreateTestTransaction(100.00m);
        var account = CreateTestAccount();
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var command = new UpdateTransactionSplitsCommand(
            TransactionId: transaction.Id,
            UserId: _userId,
            Splits:
            [
                new SplitLineInput(60.00m, "CAD", categoryId1, "Groceries"),
                new SplitLineInput(40.00m, "CAD", categoryId2, "Household"),
            ]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Amount.Should().Be(60.00m);
        result[0].CategoryId.Should().Be(categoryId1);
        result[1].Amount.Should().Be(40.00m);
        result[1].CategoryId.Should().Be(categoryId2);
    }

    [Fact]
    public async Task Handle_SplitSumMismatch_ReturnsNull()
    {
        // Arrange
        var transaction = CreateTestTransaction(100.00m);
        var account = CreateTestAccount();

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var command = new UpdateTransactionSplitsCommand(
            TransactionId: transaction.Id,
            UserId: _userId,
            Splits:
            [
                new SplitLineInput(50.00m, "CAD", Guid.NewGuid()),
                new SplitLineInput(30.00m, "CAD", Guid.NewGuid()),
            ]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsNull()
    {
        // Arrange
        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var command = new UpdateTransactionSplitsCommand(
            TransactionId: Guid.NewGuid(),
            UserId: _userId,
            Splits: [new SplitLineInput(100.00m, "CAD", Guid.NewGuid())]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsNull()
    {
        // Arrange
        var transaction = CreateTestTransaction(100.00m);
        var account = CreateTestAccount();

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var wrongUserId = Guid.NewGuid();
        var command = new UpdateTransactionSplitsCommand(
            TransactionId: transaction.Id,
            UserId: wrongUserId,
            Splits: [new SplitLineInput(100.00m, "CAD", Guid.NewGuid())]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidSplits_CallsSaveChanges()
    {
        // Arrange
        var transaction = CreateTestTransaction(100.00m);
        var account = CreateTestAccount();

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var command = new UpdateTransactionSplitsCommand(
            TransactionId: transaction.Id,
            UserId: _userId,
            Splits: [new SplitLineInput(100.00m, "CAD", Guid.NewGuid())]
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _transactionRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidSplits_ClearsPreviousSplitsFirst()
    {
        // Arrange
        var transaction = CreateTestTransaction(100.00m);
        var account = CreateTestAccount();

        // Add an existing split
        transaction.AddSplit(
            new TransactionSplit(transaction.Id, new Money(100.00m, "CAD"), Guid.NewGuid())
        );
        transaction.Splits.Should().HaveCount(1);

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var command = new UpdateTransactionSplitsCommand(
            TransactionId: transaction.Id,
            UserId: _userId,
            Splits:
            [
                new SplitLineInput(70.00m, "CAD", Guid.NewGuid()),
                new SplitLineInput(30.00m, "CAD", Guid.NewGuid()),
            ]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        transaction.Splits.Should().HaveCount(2);
    }
}
