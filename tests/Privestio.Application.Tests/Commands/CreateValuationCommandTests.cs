using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateValuation;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateValuationCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IValuationRepository> _valuationRepoMock;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Account _account;

    public CreateValuationCommandTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _valuationRepoMock = new Mock<IValuationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Valuations).Returns(_valuationRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _account = new Account(
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            "CAD",
            new Money(450000m, "CAD"),
            DateTime.UtcNow,
            _userId
        );
    }

    [Fact]
    public async Task CreateValuation_ValidCommand_ReturnsResponse()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);
        _valuationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Valuation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Valuation v, CancellationToken _) => v);

        var handler = new CreateValuationCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateValuationCommand(
            _account.Id,
            490000m,
            "CAD",
            new DateOnly(2024, 1, 1),
            "Municipal assessment",
            _userId,
            "Annual assessment"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(_account.Id);
        result.Amount.Should().Be(490000m);
        result.Currency.Should().Be("CAD");
        result.EffectiveDate.Should().Be(new DateOnly(2024, 1, 1));
        result.Source.Should().Be("Municipal assessment");
        result.Notes.Should().Be("Annual assessment");
    }

    [Fact]
    public async Task CreateValuation_CallsSaveChanges()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);
        _valuationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Valuation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Valuation v, CancellationToken _) => v);

        var handler = new CreateValuationCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateValuationCommand(
            _account.Id,
            490000m,
            "CAD",
            new DateOnly(2024, 1, 1),
            "Assessment",
            _userId
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _valuationRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Valuation>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateValuation_AccountNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new CreateValuationCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateValuationCommand(
            Guid.NewGuid(),
            490000m,
            "CAD",
            new DateOnly(2024, 1, 1),
            "Assessment",
            _userId
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateValuation_WrongOwner_ThrowsInvalidOperation()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account);

        var handler = new CreateValuationCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateValuationCommand(
            _account.Id,
            490000m,
            "CAD",
            new DateOnly(2024, 1, 1),
            "Assessment",
            Guid.NewGuid() // Different user
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }
}
