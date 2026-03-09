using FluentAssertions;
using Moq;
using Privestio.Application.Commands.UpdateContributionRoom;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ContributionRoomCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<IContributionRoomRepository> _contributionRoomRepoMock;

    public ContributionRoomCommandTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _contributionRoomRepoMock = new Mock<IContributionRoomRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ContributionRooms).Returns(_contributionRoomRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task UpdateContributionRoom_NewRoom_CreatesAndReturns()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _contributionRoomRepoMock
            .Setup(r =>
                r.GetByAccountIdAndYearAsync(accountId, 2025, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ContributionRoom?)null);

        _contributionRoomRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ContributionRoom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContributionRoom c, CancellationToken _) => c);

        var handler = new UpdateContributionRoomCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateContributionRoomCommand(
            accountId,
            userId,
            2025,
            7000m,
            1500m,
            null,
            "CAD"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.Year.Should().Be(2025);
        result.AnnualLimitAmount.Should().Be(7000m);
        result.CarryForwardAmount.Should().Be(1500m);
        result.ContributionsYtdAmount.Should().Be(0m);
        _contributionRoomRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ContributionRoom>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateContributionRoom_ExistingRoom_UpdatesFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        var existingRoom = new ContributionRoom(
            accountId,
            2025,
            new Money(6500m, "CAD"),
            new Money(1000m, "CAD")
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _contributionRoomRepoMock
            .Setup(r =>
                r.GetByAccountIdAndYearAsync(accountId, 2025, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(existingRoom);

        _contributionRoomRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ContributionRoom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContributionRoom c, CancellationToken _) => c);

        var handler = new UpdateContributionRoomCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateContributionRoomCommand(
            accountId,
            userId,
            2025,
            7000m,
            2000m,
            null,
            "CAD"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.AnnualLimitAmount.Should().Be(7000m);
        result.CarryForwardAmount.Should().Be(2000m);
        _contributionRoomRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ContributionRoom>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateContributionRoom_RecordsContribution()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            userId
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _contributionRoomRepoMock
            .Setup(r =>
                r.GetByAccountIdAndYearAsync(accountId, 2025, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((ContributionRoom?)null);

        _contributionRoomRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ContributionRoom>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContributionRoom c, CancellationToken _) => c);

        var handler = new UpdateContributionRoomCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateContributionRoomCommand(
            accountId,
            userId,
            2025,
            7000m,
            0m,
            2500m,
            "CAD"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ContributionsYtdAmount.Should().Be(2500m);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateContributionRoom_AccountNotOwned_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            DateTime.UtcNow.AddYears(-1),
            Guid.NewGuid() // Different owner
        );

        _accountRepoMock
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new UpdateContributionRoomCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateContributionRoomCommand(
            accountId,
            Guid.NewGuid(), // Different user
            2025,
            7000m,
            0m,
            null,
            "CAD"
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}
