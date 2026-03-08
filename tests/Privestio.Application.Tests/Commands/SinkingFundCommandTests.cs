using FluentAssertions;
using Moq;
using Privestio.Application.Commands.ContributeSinkingFund;
using Privestio.Application.Commands.CreateSinkingFund;
using Privestio.Application.Commands.DeleteSinkingFund;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class SinkingFundCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISinkingFundRepository> _fundRepoMock;

    public SinkingFundCommandTests()
    {
        _fundRepoMock = new Mock<ISinkingFundRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.SinkingFunds).Returns(_fundRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ── CreateSinkingFund ──

    [Fact]
    public async Task CreateSinkingFund_ValidCommand_ReturnsSinkingFundResponse()
    {
        // Arrange
        _fundRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SinkingFund>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SinkingFund f, CancellationToken _) => f);

        var handler = new CreateSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateSinkingFundCommand(
            Guid.NewGuid(),
            "Vacation Fund",
            3000m,
            DateTime.UtcNow.AddMonths(12),
            "CAD",
            null,
            null,
            "Save for vacation"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Vacation Fund");
        result.TargetAmount.Should().Be(3000m);
        result.AccumulatedAmount.Should().Be(0m);
        result.IsActive.Should().BeTrue();
        result.Notes.Should().Be("Save for vacation");
    }

    [Fact]
    public async Task CreateSinkingFund_CallsSaveChanges()
    {
        // Arrange
        _fundRepoMock
            .Setup(r => r.AddAsync(It.IsAny<SinkingFund>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SinkingFund f, CancellationToken _) => f);

        var handler = new CreateSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateSinkingFundCommand(
            Guid.NewGuid(),
            "Emergency Fund",
            10000m,
            DateTime.UtcNow.AddYears(2)
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ContributeSinkingFund ──

    [Fact]
    public async Task ContributeSinkingFund_ValidCommand_IncrementsAccumulatedAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fund = new SinkingFund(userId, "Test", new Money(5000m), DateTime.UtcNow.AddMonths(6));

        _fundRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fund);

        _fundRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<SinkingFund>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SinkingFund f, CancellationToken _) => f);

        var handler = new ContributeSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new ContributeSinkingFundCommand(Guid.NewGuid(), userId, 200m, "CAD");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.AccumulatedAmount.Should().Be(200m);
    }

    [Fact]
    public async Task ContributeSinkingFund_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _fundRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SinkingFund?)null);

        var handler = new ContributeSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new ContributeSinkingFundCommand(Guid.NewGuid(), Guid.NewGuid(), 100m);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ContributeSinkingFund_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fund = new SinkingFund(
            Guid.NewGuid(),
            "Test",
            new Money(5000m),
            DateTime.UtcNow.AddMonths(6)
        );

        _fundRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fund);

        var handler = new ContributeSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new ContributeSinkingFundCommand(Guid.NewGuid(), Guid.NewGuid(), 100m);

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    // ── DeleteSinkingFund ──

    [Fact]
    public async Task DeleteSinkingFund_ValidCommand_CallsDeleteAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fundId = Guid.NewGuid();
        var fund = new SinkingFund(userId, "Test", new Money(1000m), DateTime.UtcNow.AddMonths(3));

        _fundRepoMock
            .Setup(r => r.GetByIdAsync(fundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fund);

        var handler = new DeleteSinkingFundCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteSinkingFundCommand(fundId, userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _fundRepoMock.Verify(r => r.DeleteAsync(fundId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
