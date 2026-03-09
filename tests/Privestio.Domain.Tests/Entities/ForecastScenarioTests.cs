using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class ForecastScenarioTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesScenario()
    {
        var scenario = new ForecastScenario(UserId, "Conservative", "Low growth assumptions");

        scenario.UserId.Should().Be(UserId);
        scenario.Name.Should().Be("Conservative");
        scenario.Description.Should().Be("Low growth assumptions");
        scenario.IsDefault.Should().BeFalse();
        scenario.GrowthAssumptions.Should().BeEmpty();
        scenario.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        var act = () => new ForecastScenario(UserId, "", "desc");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => new ForecastScenario(UserId, "   ", "desc");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var scenario = new ForecastScenario(UserId, "Old Name");

        scenario.Rename("New Name");

        scenario.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var scenario = new ForecastScenario(UserId, "Original");

        var act = () => scenario.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_TrimsWhitespace()
    {
        var scenario = new ForecastScenario(UserId, "Original");

        scenario.Rename("  Trimmed  ");

        scenario.Name.Should().Be("Trimmed");
    }

    [Fact]
    public void SetDefault_SetsIsDefault()
    {
        var scenario = new ForecastScenario(UserId, "Test");

        scenario.SetDefault(true);

        scenario.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetDefault_Unset_ClearsIsDefault()
    {
        var scenario = new ForecastScenario(UserId, "Test");
        scenario.SetDefault(true);

        scenario.SetDefault(false);

        scenario.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void UpdateDescription_SetsDescription()
    {
        var scenario = new ForecastScenario(UserId, "Test");

        scenario.UpdateDescription("New description");

        scenario.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDescription_Null_ClearsDescription()
    {
        var scenario = new ForecastScenario(UserId, "Test", "old");

        scenario.UpdateDescription(null);

        scenario.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateAssumptions_ReplacesAllAssumptions()
    {
        var scenario = new ForecastScenario(UserId, "Test");
        var assumptions = new List<GrowthAssumption>
        {
            new(null, AccountType.Investment, 0.07m, 0.02m),
            new(null, AccountType.Banking, 0.02m, 0.02m),
        };

        scenario.UpdateAssumptions(assumptions);

        scenario.GrowthAssumptions.Should().HaveCount(2);
        scenario.GrowthAssumptions.First().AnnualGrowthRate.Should().Be(0.07m);
    }

    [Fact]
    public void UpdateAssumptions_EmptyList_ClearsAssumptions()
    {
        var scenario = new ForecastScenario(UserId, "Test");
        scenario.UpdateAssumptions(
            new List<GrowthAssumption> { new(null, AccountType.Investment, 0.07m, 0.02m) }
        );

        scenario.UpdateAssumptions(new List<GrowthAssumption>());

        scenario.GrowthAssumptions.Should().BeEmpty();
    }
}
