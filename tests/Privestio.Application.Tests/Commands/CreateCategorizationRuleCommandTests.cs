using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateCategorizationRule;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateCategorizationRuleCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategorizationRuleRepository> _ruleRepoMock;
    private readonly CreateCategorizationRuleCommandHandler _handler;

    public CreateCategorizationRuleCommandTests()
    {
        _ruleRepoMock = new Mock<ICategorizationRuleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.CategorizationRules).Returns(_ruleRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _ruleRepoMock
            .Setup(r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorizationRule r, CancellationToken _) => r);

        _handler = new CreateCategorizationRuleCommandHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCategorizationRuleResponse()
    {
        // Arrange
        var command = new CreateCategorizationRuleCommand(
            Name: "Grocery Rule",
            Priority: 10,
            Conditions: """{"field":"description","operator":"contains","value":"grocery"}""",
            Actions: """{"setCategoryId":"00000000-0000-0000-0000-000000000001"}""",
            UserId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Grocery Rule");
        result.Priority.Should().Be(10);
        result.Conditions.Should().Contain("grocery");
        result.Actions.Should().Contain("setCategoryId");
        result.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DisabledRule_SetsIsEnabledFalse()
    {
        // Arrange
        var command = new CreateCategorizationRuleCommand(
            Name: "Disabled Rule",
            Priority: 5,
            Conditions: """{"field":"amount","operator":"gt","value":"100"}""",
            Actions: """{"setCategoryId":"00000000-0000-0000-0000-000000000002"}""",
            UserId: Guid.NewGuid(),
            IsEnabled: false
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CreateCategorizationRuleCommand(
            Name: "Test Rule",
            Priority: 1,
            Conditions: "{}",
            Actions: "{}",
            UserId: Guid.NewGuid()
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ruleRepoMock.Verify(
            r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
