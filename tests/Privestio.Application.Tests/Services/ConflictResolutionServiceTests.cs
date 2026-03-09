using FluentAssertions;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class ConflictResolutionServiceTests
{
    private readonly ConflictResolutionService _service = new();

    #region DetectConflicts

    [Fact]
    public void DetectConflicts_IdenticalJson_ReturnsEmptyList()
    {
        // Arrange
        var json = """{"name":"Test Account","balance":100.00}""";

        // Act
        var conflicts = _service.DetectConflicts(json, json);

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectConflicts_DifferentFieldValues_DetectsConflictingFields()
    {
        // Arrange
        var localJson = """{"name":"Local Name","description":"Same"}""";
        var serverJson = """{"name":"Server Name","description":"Same"}""";

        // Act
        var conflicts = _service.DetectConflicts(localJson, serverJson);

        // Assert
        conflicts.Should().ContainSingle().Which.Should().Be("name");
    }

    [Fact]
    public void DetectConflicts_MultipleDifferences_DetectsAllConflictingFields()
    {
        // Arrange
        var localJson = """{"name":"Local","amount":50.00,"notes":"local note"}""";
        var serverJson = """{"name":"Server","amount":75.00,"notes":"server note"}""";

        // Act
        var conflicts = _service.DetectConflicts(localJson, serverJson);

        // Assert
        conflicts.Should().HaveCount(3);
        conflicts.Should().Contain("name");
        conflicts.Should().Contain("amount");
        conflicts.Should().Contain("notes");
    }

    [Fact]
    public void DetectConflicts_FieldOnlyInLocal_IsNotDetected()
    {
        // Arrange — "extra" exists only in local, not in server
        var localJson = """{"name":"Same","extra":"local only"}""";
        var serverJson = """{"name":"Same"}""";

        // Act
        var conflicts = _service.DetectConflicts(localJson, serverJson);

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectConflicts_InvalidJson_ReturnsWildcard()
    {
        // Arrange
        var localJson = "not valid json";
        var serverJson = """{"name":"Test"}""";

        // Act
        var conflicts = _service.DetectConflicts(localJson, serverJson);

        // Assert
        conflicts.Should().ContainSingle().Which.Should().Be("*");
    }

    [Fact]
    public void DetectConflicts_BothInvalidJson_ReturnsWildcard()
    {
        // Arrange
        var localJson = "{broken";
        var serverJson = "{also broken";

        // Act
        var conflicts = _service.DetectConflicts(localJson, serverJson);

        // Assert
        conflicts.Should().ContainSingle().Which.Should().Be("*");
    }

    #endregion

    #region AutoResolve

    [Fact]
    public void AutoResolve_NoConflicts_ResolvesWithKeepServer()
    {
        // Arrange
        var json = """{"name":"Same","description":"Same"}""";
        var conflict = new SyncConflict("Account", Guid.NewGuid(), json, json);

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeTrue();
        conflict.Status.Should().Be("Resolved");
        conflict.Resolution.Should().Be("KeepServer");
    }

    [Fact]
    public void AutoResolve_FinancialFieldConflict_ReturnsFalse()
    {
        // Arrange — "amount" is a financial field
        var localJson = """{"amount":100.00,"name":"Test"}""";
        var serverJson = """{"amount":200.00,"name":"Test"}""";
        var conflict = new SyncConflict("Transaction", Guid.NewGuid(), localJson, serverJson);

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeFalse();
        conflict.Status.Should().Be("Pending");
    }

    [Theory]
    [InlineData("balance")]
    [InlineData("price")]
    [InlineData("total")]
    [InlineData("budgetedAmount")]
    [InlineData("actualAmount")]
    [InlineData("statementBalance")]
    [InlineData("targetAmount")]
    [InlineData("currentAmount")]
    public void AutoResolve_AnyFinancialField_ReturnsFalse(string financialField)
    {
        // Arrange
        var localJson = $$"""{"{{financialField}}":100}""";
        var serverJson = $$"""{"{{financialField}}":200}""";
        var conflict = new SyncConflict("Entity", Guid.NewGuid(), localJson, serverJson);

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeFalse();
        conflict.Status.Should().Be("Pending");
    }

    [Fact]
    public void AutoResolve_NonFinancialFieldConflict_ResolvesWithKeepServer()
    {
        // Arrange — "name" and "notes" are not financial fields
        var localJson = """{"name":"Local Name","notes":"local notes"}""";
        var serverJson = """{"name":"Server Name","notes":"server notes"}""";
        var conflict = new SyncConflict("Account", Guid.NewGuid(), localJson, serverJson);

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeTrue();
        conflict.Status.Should().Be("Resolved");
        conflict.Resolution.Should().Be("KeepServer");
    }

    [Fact]
    public void AutoResolve_WildcardConflict_ReturnsFalse()
    {
        // Arrange — invalid JSON triggers "*" wildcard conflict
        var conflict = new SyncConflict("Account", Guid.NewGuid(), "bad json", """{"name":"ok"}""");

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeFalse();
        conflict.Status.Should().Be("Pending");
    }

    [Fact]
    public void AutoResolve_MixOfFinancialAndNonFinancial_ReturnsFalse()
    {
        // Arrange — "name" is non-financial but "amount" is financial
        var localJson = """{"name":"Local","amount":50.00}""";
        var serverJson = """{"name":"Server","amount":100.00}""";
        var conflict = new SyncConflict("Transaction", Guid.NewGuid(), localJson, serverJson);

        // Act
        var result = _service.AutoResolve(conflict);

        // Assert
        result.Should().BeFalse();
        conflict.Status.Should().Be("Pending");
    }

    #endregion

    #region QueueForUserResolution

    [Fact]
    public void QueueForUserResolution_ConflictRemainsPending()
    {
        // Arrange
        var conflict = new SyncConflict(
            "Account",
            Guid.NewGuid(),
            """{"name":"Local"}""",
            """{"name":"Server"}"""
        );

        // Act
        _service.QueueForUserResolution(conflict);

        // Assert
        conflict.Status.Should().Be("Pending");
        conflict.Resolution.Should().BeNull();
        conflict.ResolvedAt.Should().BeNull();
    }

    #endregion
}
