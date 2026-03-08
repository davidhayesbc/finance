using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateRecurringTransaction;
using Privestio.Application.Commands.DeleteRecurringTransaction;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class RecurringTransactionCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepoMock;

    public RecurringTransactionCommandTests()
    {
        _recurringRepoMock = new Mock<IRecurringTransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.RecurringTransactions).Returns(_recurringRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateRecurringTransaction_ValidCommand_ReturnsResponse()
    {
        // Arrange
        _recurringRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransaction r, CancellationToken _) => r);

        var handler = new CreateRecurringTransactionCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateRecurringTransactionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Monthly Rent",
            1500m,
            "Debit",
            "Monthly",
            DateTime.UtcNow,
            null,
            "CAD",
            null,
            null,
            "Rent payment"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Monthly Rent");
        result.Amount.Should().Be(1500m);
        result.TransactionType.Should().Be("Debit");
        result.Frequency.Should().Be("Monthly");
        result.IsActive.Should().BeTrue();
        result.Notes.Should().Be("Rent payment");
    }

    [Fact]
    public async Task CreateRecurringTransaction_CallsSaveChanges()
    {
        // Arrange
        _recurringRepoMock
            .Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransaction r, CancellationToken _) => r);

        var handler = new CreateRecurringTransactionCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateRecurringTransactionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Weekly Groceries",
            100m,
            "Debit",
            "Weekly",
            DateTime.UtcNow
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRecurringTransaction_ValidCommand_CallsDeleteAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var recurring = new RecurringTransaction(
            userId,
            Guid.NewGuid(),
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        _recurringRepoMock
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);

        var handler = new DeleteRecurringTransactionCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteRecurringTransactionCommand(recurringId, userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _recurringRepoMock.Verify(
            r => r.DeleteAsync(recurringId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteRecurringTransaction_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var recurring = new RecurringTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        _recurringRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);

        var handler = new DeleteRecurringTransactionCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteRecurringTransactionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}
