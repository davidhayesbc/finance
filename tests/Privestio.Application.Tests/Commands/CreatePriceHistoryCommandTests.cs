using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreatePriceHistory;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreatePriceHistoryCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Security _security;

    public CreatePriceHistoryCommandTests()
    {
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.PriceHistories).Returns(_priceHistoryRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard S&P 500 Index ETF");
    }

    [Fact]
    public async Task CreatePriceHistory_NewEntries_ReturnsCreated()
    {
        // Arrange
        _priceHistoryRepoMock
            .Setup(r =>
                r.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(Guid, DateOnly)>());

        var handler = new CreatePriceHistoryCommandHandler(
            _unitOfWorkMock.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWorkMock, [_security])
        );
        var command = new CreatePriceHistoryCommand([
            new PriceHistoryEntry("VFV.TO", 105.23m, "CAD", new DateOnly(2024, 1, 2), "Yahoo"),
            new PriceHistoryEntry("VFV.TO", 104.87m, "CAD", new DateOnly(2024, 1, 3), "Yahoo"),
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Symbol.Should().Be("VFV.TO");
        result[0].SecurityId.Should().Be(_security.Id);
        result[0].Price.Should().Be(105.23m);
        result[1].Price.Should().Be(104.87m);
    }

    [Fact]
    public async Task CreatePriceHistory_SkipsDuplicates()
    {
        // Arrange
        var existingKeys = new HashSet<(Guid, DateOnly)>
        {
            (_security.Id, new DateOnly(2024, 1, 2)),
        };
        _priceHistoryRepoMock
            .Setup(r =>
                r.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existingKeys);

        var handler = new CreatePriceHistoryCommandHandler(
            _unitOfWorkMock.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWorkMock, [_security])
        );
        var command = new CreatePriceHistoryCommand([
            new PriceHistoryEntry("VFV.TO", 105.23m, "CAD", new DateOnly(2024, 1, 2), "Yahoo"), // existing
            new PriceHistoryEntry("VFV.TO", 104.87m, "CAD", new DateOnly(2024, 1, 3), "Yahoo"), // new
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].AsOfDate.Should().Be(new DateOnly(2024, 1, 3));
    }

    [Fact]
    public async Task CreatePriceHistory_AllDuplicates_DoesNotSave()
    {
        // Arrange
        var existingKeys = new HashSet<(Guid, DateOnly)>
        {
            (_security.Id, new DateOnly(2024, 1, 2)),
        };
        _priceHistoryRepoMock
            .Setup(r =>
                r.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existingKeys);

        var handler = new CreatePriceHistoryCommandHandler(
            _unitOfWorkMock.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWorkMock, [_security])
        );
        var command = new CreatePriceHistoryCommand([
            new PriceHistoryEntry("VFV.TO", 105.23m, "CAD", new DateOnly(2024, 1, 2), "Yahoo"),
        ]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePriceHistory_CallsAddRangeAndSave()
    {
        // Arrange
        _priceHistoryRepoMock
            .Setup(r =>
                r.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(Guid, DateOnly)>());

        var handler = new CreatePriceHistoryCommandHandler(
            _unitOfWorkMock.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWorkMock, [_security])
        );
        var command = new CreatePriceHistoryCommand([
            new PriceHistoryEntry("VFV.TO", 105.23m, "CAD", new DateOnly(2024, 1, 2), "Yahoo"),
        ]);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _priceHistoryRepoMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<PriceHistory>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
