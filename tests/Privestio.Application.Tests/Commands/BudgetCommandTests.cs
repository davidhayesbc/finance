using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateBudget;
using Privestio.Application.Commands.DeleteBudget;
using Privestio.Application.Commands.UpdateBudget;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class BudgetCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBudgetRepository> _budgetRepoMock;

    public BudgetCommandTests()
    {
        _budgetRepoMock = new Mock<IBudgetRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Budgets).Returns(_budgetRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ── CreateBudget ──

    [Fact]
    public async Task CreateBudget_ValidCommand_ReturnsBudgetResponse()
    {
        // Arrange
        _budgetRepoMock
            .Setup(r =>
                r.GetByUserCategoryPeriodAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget b, CancellationToken _) => b);

        var handler = new CreateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateBudgetCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2025,
            6,
            500m,
            "CAD",
            true,
            "Groceries budget"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(2025);
        result.Month.Should().Be(6);
        result.Amount.Should().Be(500m);
        result.Currency.Should().Be("CAD");
        result.RolloverEnabled.Should().BeTrue();
        result.Notes.Should().Be("Groceries budget");
    }

    [Fact]
    public async Task CreateBudget_DuplicatePeriod_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        _budgetRepoMock
            .Setup(r =>
                r.GetByUserCategoryPeriodAsync(
                    userId,
                    categoryId,
                    2025,
                    6,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Budget(userId, categoryId, 2025, 6, new Money(300m)));

        var handler = new CreateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateBudgetCommand(userId, categoryId, 2025, 6, 500m);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBudget_CallsSaveChanges()
    {
        // Arrange
        _budgetRepoMock
            .Setup(r =>
                r.GetByUserCategoryPeriodAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget b, CancellationToken _) => b);

        var handler = new CreateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateBudgetCommand(Guid.NewGuid(), Guid.NewGuid(), 2025, 1, 100m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateBudget ──

    [Fact]
    public async Task UpdateBudget_ValidCommand_ReturnsUpdatedBudgetResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = new Budget(userId, Guid.NewGuid(), 2025, 6, new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget b, CancellationToken _) => b);

        var handler = new UpdateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateBudgetCommand(budgetId, userId, 750m, "CAD", true, "Updated");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Amount.Should().Be(750m);
        result.RolloverEnabled.Should().BeTrue();
        result.Notes.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateBudget_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var handler = new UpdateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateBudgetCommand(Guid.NewGuid(), Guid.NewGuid(), 500m);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBudget_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = new Budget(Guid.NewGuid(), Guid.NewGuid(), 2025, 6, new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var handler = new UpdateBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateBudgetCommand(budgetId, Guid.NewGuid(), 500m);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    // ── DeleteBudget ──

    [Fact]
    public async Task DeleteBudget_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = new Budget(userId, Guid.NewGuid(), 2025, 6, new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var handler = new DeleteBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteBudgetCommand(budgetId, userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(
            r => r.DeleteAsync(budgetId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBudget_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = new Budget(Guid.NewGuid(), Guid.NewGuid(), 2025, 6, new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var handler = new DeleteBudgetCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteBudgetCommand(budgetId, Guid.NewGuid());

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}
