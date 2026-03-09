using FluentAssertions;
using Moq;
using Privestio.Application.Commands.GenerateAmortizationSchedule;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class AmortizationCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IAmortizationEntryRepository> _amortizationRepoMock;
    private readonly AmortizationScheduleService _amortizationService;

    public AmortizationCommandTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _amortizationRepoMock = new Mock<IAmortizationEntryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AmortizationEntries).Returns(_amortizationRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _amortizationService = new AmortizationScheduleService();
    }

    [Fact]
    public async Task GenerateAmortizationSchedule_ValidCommand_ReturnsSchedule()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            "CAD",
            new Money(200000m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GenerateAmortizationScheduleCommandHandler(
            _unitOfWorkMock.Object,
            _amortizationService
        );
        var command = new GenerateAmortizationScheduleCommand(
            userId,
            accountId,
            200000m,
            5.0m,
            300,
            1169.18m,
            new DateOnly(2025, 1, 1),
            "CAD"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.Entries.Should().NotBeEmpty();
        result.Currency.Should().Be("CAD");
        result.TotalPrincipal.Should().BeGreaterThan(0);
        result.TotalInterest.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAmortizationSchedule_DeletesExistingEntries()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            "CAD",
            new Money(200000m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GenerateAmortizationScheduleCommandHandler(
            _unitOfWorkMock.Object,
            _amortizationService
        );
        var command = new GenerateAmortizationScheduleCommand(
            userId,
            accountId,
            100000m,
            4.0m,
            60,
            1843.49m,
            new DateOnly(2025, 1, 1),
            "CAD"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _amortizationRepoMock.Verify(
            r => r.DeleteByAccountIdAsync(accountId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _amortizationRepoMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<AmortizationEntry>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAmortizationSchedule_AccountNotOwned_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account(
            "Mortgage",
            AccountType.Loan,
            AccountSubType.Mortgage,
            "CAD",
            new Money(200000m),
            DateTime.UtcNow.AddYears(-1),
            Guid.NewGuid() // Different owner
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GenerateAmortizationScheduleCommandHandler(
            _unitOfWorkMock.Object,
            _amortizationService
        );
        var command = new GenerateAmortizationScheduleCommand(
            Guid.NewGuid(), // Different user
            accountId,
            200000m,
            5.0m,
            300,
            1169.18m,
            new DateOnly(2025, 1, 1),
            "CAD"
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}
