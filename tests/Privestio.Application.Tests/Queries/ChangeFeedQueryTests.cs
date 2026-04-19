using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetChangesSince;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class ChangeFeedQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ISyncTombstoneRepository> _tombstoneRepoMock;
    private readonly Mock<ISyncCheckpointRepository> _checkpointRepoMock;
    private readonly GetChangesSinceQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private const string DeviceId = "device-001";

    public ChangeFeedQueryTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _tombstoneRepoMock = new Mock<ISyncTombstoneRepository>();
        _checkpointRepoMock = new Mock<ISyncCheckpointRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SyncTombstones).Returns(_tombstoneRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SyncCheckpoints).Returns(_checkpointRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Default: no existing checkpoint
        _checkpointRepoMock
            .Setup(r => r.GetByUserAndDeviceAsync(_userId, DeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint?)null);

        _checkpointRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint cp, CancellationToken _) => cp);

        _handler = new GetChangesSinceQueryHandler(_unitOfWorkMock.Object);
    }

    private void SetupEmptyDefaults(DateTime? sinceToken = null)
    {
        var since = sinceToken ?? DateTime.MinValue;

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>().AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>().AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(since, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone>().AsReadOnly());
    }

    private static Account CreateAccount(Guid ownerId, DateTime createdAt, DateTime updatedAt)
    {
        var account = new Account(
            "Test Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(1000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ownerId
        );

        // Use reflection to set the timestamps since they are set in the BaseEntity constructor
        SetEntityTimestamps(account, createdAt, updatedAt);
        return account;
    }

    private static Transaction CreateTransaction(
        Guid accountId,
        DateTime createdAt,
        DateTime updatedAt
    )
    {
        var transaction = new Transaction(
            accountId,
            DateTime.UtcNow,
            new Money(50m, "CAD"),
            "Test Transaction",
            TransactionType.Debit
        );

        SetEntityTimestamps(transaction, createdAt, updatedAt);
        return transaction;
    }

    private static void SetEntityTimestamps(
        BaseEntity entity,
        DateTime createdAt,
        DateTime updatedAt
    )
    {
        // BaseEntity.CreatedAt has a protected setter, use reflection
        var createdAtProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!;
        createdAtProp.SetValue(entity, createdAt);

        entity.UpdatedAt = updatedAt;
    }

    [Fact]
    public async Task Handle_NothingChangedSinceToken_ReturnsEmptyChanges()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow;
        SetupEmptyDefaults(sinceToken);

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Changes.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AccountCreatedAfterToken_ReturnsCreatedAccount()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-1);
        var accountCreatedAt = DateTime.UtcNow.AddMinutes(-30);
        var account = CreateAccount(_userId, accountCreatedAt, accountCreatedAt);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account }.AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>().AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone>().AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Changes.Should().ContainSingle();
        var change = result.Changes[0];
        change.EntityType.Should().Be("Account");
        change.EntityId.Should().Be(account.Id);
        change.ChangeType.Should().Be("Created");
        change.Payload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_TransactionUpdatedAfterToken_ReturnsUpdatedTransaction()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-1);
        var createdBefore = DateTime.UtcNow.AddDays(-5); // created well before the token
        var updatedAfter = DateTime.UtcNow.AddMinutes(-15); // updated after the token
        var accountId = Guid.NewGuid();
        var transaction = CreateTransaction(accountId, createdBefore, updatedAfter);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>().AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { transaction }.AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone>().AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Changes.Should().ContainSingle();
        var change = result.Changes[0];
        change.EntityType.Should().Be("Transaction");
        change.EntityId.Should().Be(transaction.Id);
        change.ChangeType.Should().Be("Updated");
        change.Payload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_TombstoneExists_ReturnsDeletedEntity()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-1);
        var deletedEntityId = Guid.NewGuid();
        var tombstone = new SyncTombstone("Account", deletedEntityId);

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>().AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>().AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone> { tombstone }.AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Changes.Should().ContainSingle();
        var change = result.Changes[0];
        change.EntityType.Should().Be("Account");
        change.EntityId.Should().Be(deletedEntityId);
        change.ChangeType.Should().Be("Deleted");
        change.Payload.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoExistingCheckpoint_CreatesNewCheckpoint()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-1);
        SetupEmptyDefaults(sinceToken);

        _checkpointRepoMock
            .Setup(r => r.GetByUserAndDeviceAsync(_userId, DeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncCheckpoint?)null);

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _checkpointRepoMock.Verify(
            r =>
                r.AddAsync(
                    It.Is<SyncCheckpoint>(cp => cp.UserId == _userId && cp.DeviceId == DeviceId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCheckpoint_DoesNotCreateNew()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-1);
        SetupEmptyDefaults(sinceToken);

        var existingCheckpoint = new SyncCheckpoint(_userId, DeviceId);
        _checkpointRepoMock
            .Setup(r => r.GetByUserAndDeviceAsync(_userId, DeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCheckpoint);

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _checkpointRepoMock.Verify(
            r => r.AddAsync(It.IsAny<SyncCheckpoint>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_MultipleChanges_OrderedByChangedAt()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddHours(-2);

        // Account created 90 minutes ago (earliest)
        var accountTime = DateTime.UtcNow.AddMinutes(-90);
        var account = CreateAccount(_userId, accountTime, accountTime);

        // Transaction updated 30 minutes ago (middle)
        var transactionTime = DateTime.UtcNow.AddMinutes(-30);
        var accountId = Guid.NewGuid();
        var transaction = CreateTransaction(accountId, sinceToken.AddMinutes(-60), transactionTime);

        // Tombstone is recent (latest) -- created via UtcNow in constructor
        var tombstone = new SyncTombstone("Category", Guid.NewGuid());

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account }.AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction> { transaction }.AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone> { tombstone }.AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Changes.Should().HaveCount(3);
        result.Changes.Should().BeInAscendingOrder(c => c.ChangedAt);

        // The account change (90 min ago) should come first
        result.Changes[0].EntityType.Should().Be("Account");
        // The transaction change (30 min ago) should come second
        result.Changes[1].EntityType.Should().Be("Transaction");
        // The tombstone (just now) should come last
        result.Changes[2].EntityType.Should().Be("Category");
    }

    [Fact]
    public async Task Handle_MoreThan100Changes_HasMoreIsTrue()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddDays(-1);

        // Create 101 accounts, all updated after the token
        var accounts = Enumerable
            .Range(0, 101)
            .Select(i =>
            {
                var time = DateTime.UtcNow.AddMinutes(-i);
                return CreateAccount(_userId, time, time);
            })
            .ToList();

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>().AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone>().AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasMore.Should().BeTrue();
        result.Changes.Should().HaveCount(100);
    }

    [Fact]
    public async Task Handle_Exactly100Changes_HasMoreIsFalse()
    {
        // Arrange
        var sinceToken = DateTime.UtcNow.AddDays(-1);

        var accounts = Enumerable
            .Range(0, 100)
            .Select(i =>
            {
                var time = DateTime.UtcNow.AddMinutes(-i);
                return CreateAccount(_userId, time, time);
            })
            .ToList();

        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.AsReadOnly());

        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    _userId,
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>().AsReadOnly());

        _tombstoneRepoMock
            .Setup(r => r.GetSinceForUserAsync(sinceToken, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncTombstone>().AsReadOnly());

        var query = new GetChangesSinceQuery(_userId, DeviceId, sinceToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasMore.Should().BeFalse();
        result.Changes.Should().HaveCount(100);
    }
}
