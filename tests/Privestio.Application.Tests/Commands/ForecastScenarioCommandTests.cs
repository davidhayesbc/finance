using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateForecastScenario;
using Privestio.Application.Commands.DeleteForecastScenario;
using Privestio.Application.Commands.UpdateForecastScenario;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Requests;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ForecastScenarioCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IForecastScenarioRepository> _forecastRepoMock;

    public ForecastScenarioCommandTests()
    {
        _forecastRepoMock = new Mock<IForecastScenarioRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ForecastScenarios).Returns(_forecastRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ── CreateForecastScenario ──

    [Fact]
    public async Task CreateForecastScenario_ValidCommand_ReturnsResponse()
    {
        // Arrange
        _forecastRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ForecastScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario s, CancellationToken _) => s);

        var handler = new CreateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateForecastScenarioCommand(
            Guid.NewGuid(),
            "Optimistic Growth",
            "High growth scenario",
            new List<GrowthAssumptionDto>()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Optimistic Growth");
        result.Description.Should().Be("High growth scenario");
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateForecastScenario_WithGrowthAssumptions_MapsCorrectly()
    {
        // Arrange
        _forecastRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ForecastScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario s, CancellationToken _) => s);

        var accountId = Guid.NewGuid();
        var assumptions = new List<GrowthAssumptionDto>
        {
            new(accountId, null, 7.0m, 2.0m),
            new(null, "Investment", 8.0m, 2.5m),
        };

        var handler = new CreateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateForecastScenarioCommand(
            Guid.NewGuid(),
            "Mixed Scenario",
            null,
            assumptions
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.GrowthAssumptions.Should().HaveCount(2);
        result.GrowthAssumptions[0].AccountId.Should().Be(accountId);
        result.GrowthAssumptions[0].AnnualGrowthRate.Should().Be(7.0m);
        result.GrowthAssumptions[0].AnnualInflationRate.Should().Be(2.0m);
        result.GrowthAssumptions[1].AccountType.Should().Be("Investment");
        result.GrowthAssumptions[1].AnnualGrowthRate.Should().Be(8.0m);
    }

    [Fact]
    public async Task CreateForecastScenario_CallsSaveChanges()
    {
        // Arrange
        _forecastRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ForecastScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario s, CancellationToken _) => s);

        var handler = new CreateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateForecastScenarioCommand(
            Guid.NewGuid(),
            "Test Scenario",
            null,
            new List<GrowthAssumptionDto>()
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateForecastScenario ──

    [Fact]
    public async Task UpdateForecastScenario_ValidCommand_UpdatesFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();
        var scenario = new ForecastScenario(userId, "Original", "Original desc");

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        _forecastRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ForecastScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario s, CancellationToken _) => s);

        var handler = new UpdateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateForecastScenarioCommand(
            scenarioId,
            userId,
            "Updated Name",
            "Updated description",
            new List<GrowthAssumptionDto>(),
            true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateForecastScenario_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ForecastScenario?)null);

        var handler = new UpdateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateForecastScenarioCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Name",
            null,
            new List<GrowthAssumptionDto>(),
            false
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateForecastScenario_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var scenarioId = Guid.NewGuid();
        var scenario = new ForecastScenario(Guid.NewGuid(), "Test", "Desc");

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var handler = new UpdateForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new UpdateForecastScenarioCommand(
            scenarioId,
            Guid.NewGuid(),
            "Name",
            null,
            new List<GrowthAssumptionDto>(),
            false
        );

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    // ── DeleteForecastScenario ──

    [Fact]
    public async Task DeleteForecastScenario_ValidCommand_CallsDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();
        var scenario = new ForecastScenario(userId, "Test", "Desc");

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var handler = new DeleteForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteForecastScenarioCommand(scenarioId, userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _forecastRepoMock.Verify(
            r => r.DeleteAsync(scenarioId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteForecastScenario_WrongUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var scenarioId = Guid.NewGuid();
        var scenario = new ForecastScenario(Guid.NewGuid(), "Test", "Desc");

        _forecastRepoMock
            .Setup(r => r.GetByIdAsync(scenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var handler = new DeleteForecastScenarioCommandHandler(_unitOfWorkMock.Object);
        var command = new DeleteForecastScenarioCommand(scenarioId, Guid.NewGuid());

        // Act & Assert
        await handler
            .Invoking(h => h.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}
