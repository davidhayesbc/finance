using FluentAssertions;
using Moq;
using Privestio.Application.Commands.DeletePriceHistory;
using Privestio.Application.Commands.UpdatePriceHistory;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class UpdatePriceHistoryCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;

    public UpdatePriceHistoryCommandTests()
    {
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.PriceHistories).Returns(_priceHistoryRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task UpdatePriceHistory_ExistingEntry_UpdatesAndReturnsResponse()
    {
        // Arrange
        var security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard S&P 500 Index ETF");
        var entry = new PriceHistory(
            security.Id,
            "VFV.TO",
            "VFV.TO",
            new Money(100m, "CAD"),
            new DateOnly(2024, 1, 2),
            "YahooFinance"
        );

        _priceHistoryRepoMock
            .Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _priceHistoryRepoMock
            .Setup(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var handler = new UpdatePriceHistoryCommandHandler(_unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdatePriceHistoryCommand(entry.Id, 120m, "CAD"),
            CancellationToken.None
        );

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(120m);
        result.Source.Should().Be("Manual");
    }

    [Fact]
    public async Task UpdatePriceHistory_NotFound_ReturnsNull()
    {
        // Arrange
        _priceHistoryRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceHistory?)null);

        var handler = new UpdatePriceHistoryCommandHandler(_unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new UpdatePriceHistoryCommand(Guid.NewGuid(), 120m, "CAD"),
            CancellationToken.None
        );

        // Assert
        result.Should().BeNull();
    }
}

public class DeletePriceHistoryCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;

    public DeletePriceHistoryCommandTests()
    {
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.PriceHistories).Returns(_priceHistoryRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task DeletePriceHistory_ExistingEntry_ReturnsTrueAndDeletes()
    {
        // Arrange
        var security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard S&P 500 Index ETF");
        var entry = new PriceHistory(
            security.Id,
            "VFV.TO",
            "VFV.TO",
            new Money(100m, "CAD"),
            new DateOnly(2024, 1, 2),
            "YahooFinance"
        );

        _priceHistoryRepoMock
            .Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        _priceHistoryRepoMock
            .Setup(r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeletePriceHistoryCommandHandler(_unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new DeletePriceHistoryCommand(entry.Id),
            CancellationToken.None
        );

        // Assert
        result.Should().BeTrue();
        _priceHistoryRepoMock.Verify(
            r => r.DeleteAsync(entry.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePriceHistory_NotFound_ReturnsFalse()
    {
        // Arrange
        _priceHistoryRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceHistory?)null);

        var handler = new DeletePriceHistoryCommandHandler(_unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(
            new DeletePriceHistoryCommand(Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        result.Should().BeFalse();
        _priceHistoryRepoMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
