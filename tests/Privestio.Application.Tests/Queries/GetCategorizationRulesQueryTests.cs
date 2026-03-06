using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetCategorizationRules;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class GetCategorizationRulesQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategorizationRuleRepository> _ruleRepoMock;
    private readonly GetCategorizationRulesQueryHandler _handler;

    public GetCategorizationRulesQueryTests()
    {
        _ruleRepoMock = new Mock<ICategorizationRuleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.CategorizationRules).Returns(_ruleRepoMock.Object);

        _handler = new GetCategorizationRulesQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsRulesForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rules = new List<CategorizationRule>
        {
            new("Grocery Rule", 10, "{}", "{}", userId),
            new("Gas Rule", 20, "{}", "{}", userId),
        };

        _ruleRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var query = new GetCategorizationRulesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Grocery Rule");
        result[0].Priority.Should().Be(10);
        result[0].IsEnabled.Should().BeTrue();
        result[1].Name.Should().Be("Gas Rule");
        result[1].Priority.Should().Be(20);
    }

    [Fact]
    public async Task Handle_NoRules_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _ruleRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        var query = new GetCategorizationRulesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
