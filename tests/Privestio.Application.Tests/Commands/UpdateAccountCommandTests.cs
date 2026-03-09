using FluentAssertions;
using Moq;
using Privestio.Application.Commands.UpdateAccount;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class UpdateAccountCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Account _account;

    public UpdateAccountCommandTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _account = new Account(
            "RBC Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(1500m, "CAD"),
            DateTime.UtcNow,
            _userId,
            "RBC"
        );
    }

    [Fact]
    public async Task UpdateAccount_ValidCommand_ReturnsUpdatedResponse()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);
        _accountRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken _) => a);

        var handler = new UpdateAccountCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(
            _account.Id,
            _userId,
            "RBC Primary Chequing",
            "Royal Bank",
            "Main account",
            false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("RBC Primary Chequing");
        result.Notes.Should().Be("Main account");
    }

    [Fact]
    public async Task UpdateAccount_AccountNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new UpdateAccountCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(Guid.NewGuid(), _userId, "Test", null, null, false);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAccount_WrongOwner_ThrowsInvalidOperation()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);

        var handler = new UpdateAccountCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(
            _account.Id,
            Guid.NewGuid(), // Different user
            "Test",
            null,
            null,
            false
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAccount_CallsSaveChanges()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);
        _accountRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken _) => a);

        var handler = new UpdateAccountCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(
            _account.Id,
            _userId,
            "Updated Name",
            null,
            null,
            false
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _accountRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
