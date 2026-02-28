using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateAccount;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateAccountCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly CreateAccountCommandHandler _handler;

    public CreateAccountCommandTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _accountRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken _) => a);

        _handler = new CreateAccountCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAccountResponse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var command = new CreateAccountCommand(
            Name: "Test Chequing",
            AccountType: "Banking",
            AccountSubType: "Chequing",
            Currency: "CAD",
            OpeningBalance: 1000.00m,
            OpeningDate: DateTime.UtcNow.AddYears(-1),
            OwnerId: ownerId,
            Institution: "RBC");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Chequing");
        result.AccountType.Should().Be("Banking");
        result.AccountSubType.Should().Be("Chequing");
        result.Currency.Should().Be("CAD");
        result.OpeningBalance.Should().Be(1000.00m);
        result.Institution.Should().Be("RBC");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CreateAccountCommand(
            Name: "TFSA",
            AccountType: "Investment",
            AccountSubType: "TFSA",
            Currency: "CAD",
            OpeningBalance: 0m,
            OpeningDate: DateTime.UtcNow.AddYears(-2),
            OwnerId: Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
