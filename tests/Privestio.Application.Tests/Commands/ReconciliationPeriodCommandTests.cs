using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateReconciliationPeriod;
using Privestio.Application.Commands.LockReconciliationPeriod;
using Privestio.Application.Commands.UnlockReconciliationPeriod;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ReconciliationPeriodCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReconciliationPeriodRepository> _reconciliationRepoMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;

    public ReconciliationPeriodCommandTests()
    {
        _reconciliationRepoMock = new Mock<IReconciliationPeriodRepository>();
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ReconciliationPeriods).Returns(_reconciliationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ── CreateReconciliationPeriod ──

    [Fact]
    public async Task CreateReconciliationPeriod_ValidCommand_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _reconciliationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ReconciliationPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationPeriod p, CancellationToken _) => p);

        var handler = new CreateReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateReconciliationPeriodCommand(
            userId,
            accountId,
            new DateOnly(2025, 6, 30),
            1500.00m,
            "CAD",
            "June statement"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.StatementDate.Should().Be(new DateOnly(2025, 6, 30));
        result.StatementBalanceAmount.Should().Be(1500.00m);
        result.Currency.Should().Be("CAD");
        result.Status.Should().Be("Open");
        result.Notes.Should().Be("June statement");
    }

    [Fact]
    public async Task CreateReconciliationPeriod_AccountNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new CreateReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateReconciliationPeriodCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2025, 6, 30),
            1500.00m,
            "CAD",
            null
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    // ── LockReconciliationPeriod ──

    [Fact]
    public async Task LockReconciliationPeriod_BalancedPeriod_LocksSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var period = new ReconciliationPeriod(
            Guid.NewGuid(),
            new DateOnly(2025, 6, 30),
            new Money(1500m)
        );
        period.MarkBalanced();

        _reconciliationRepoMock
            .Setup(r => r.GetByIdAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        _reconciliationRepoMock
            .Setup(r =>
                r.UpdateAsync(It.IsAny<ReconciliationPeriod>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ReconciliationPeriod p, CancellationToken _) => p);

        var handler = new LockReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new LockReconciliationPeriodCommand(periodId, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Locked");
        result.LockedByUserId.Should().Be(userId);
        result.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LockReconciliationPeriod_OpenPeriod_ThrowsInvalidOperationException()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new ReconciliationPeriod(
            Guid.NewGuid(),
            new DateOnly(2025, 6, 30),
            new Money(1500m)
        );
        // Period is in Open status - not Balanced, so Lock should throw

        _reconciliationRepoMock
            .Setup(r => r.GetByIdAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var handler = new LockReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new LockReconciliationPeriodCommand(periodId, Guid.NewGuid());

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LockReconciliationPeriod_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _reconciliationRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationPeriod?)null);

        var handler = new LockReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new LockReconciliationPeriodCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    // ── UnlockReconciliationPeriod ──

    [Fact]
    public async Task UnlockReconciliationPeriod_LockedPeriod_UnlocksSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var period = new ReconciliationPeriod(
            Guid.NewGuid(),
            new DateOnly(2025, 6, 30),
            new Money(1500m)
        );
        period.MarkBalanced();
        period.Lock(userId);

        _reconciliationRepoMock
            .Setup(r => r.GetByIdAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        _reconciliationRepoMock
            .Setup(r =>
                r.UpdateAsync(It.IsAny<ReconciliationPeriod>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ReconciliationPeriod p, CancellationToken _) => p);

        var handler = new UnlockReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new UnlockReconciliationPeriodCommand(periodId, userId, "Correction needed");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Open");
        result.UnlockReason.Should().Be("Correction needed");
        result.LockedAt.Should().BeNull();
        result.LockedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task UnlockReconciliationPeriod_NotLocked_ThrowsInvalidOperationException()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var period = new ReconciliationPeriod(
            Guid.NewGuid(),
            new DateOnly(2025, 6, 30),
            new Money(1500m)
        );
        // Period is Open - Unlock should throw

        _reconciliationRepoMock
            .Setup(r => r.GetByIdAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var handler = new UnlockReconciliationPeriodCommandHandler(_unitOfWorkMock.Object);
        var command = new UnlockReconciliationPeriodCommand(periodId, Guid.NewGuid(), "Reason");

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }
}
