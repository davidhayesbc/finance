using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.ValueObjects;

public class GrowthAssumptionTests
{
    [Fact]
    public void Constructor_WithAccountId_CreatesInstance()
    {
        var accountId = Guid.NewGuid();
        var assumption = new GrowthAssumption(accountId, null, 0.07m, 0.02m);

        assumption.AccountId.Should().Be(accountId);
        assumption.AccountType.Should().BeNull();
        assumption.AnnualGrowthRate.Should().Be(0.07m);
        assumption.AnnualInflationRate.Should().Be(0.02m);
    }

    [Fact]
    public void Constructor_WithAccountType_CreatesInstance()
    {
        var assumption = new GrowthAssumption(null, AccountType.Investment, 0.10m, 0.03m);

        assumption.AccountId.Should().BeNull();
        assumption.AccountType.Should().Be(AccountType.Investment);
        assumption.AnnualGrowthRate.Should().Be(0.10m);
        assumption.AnnualInflationRate.Should().Be(0.03m);
    }

    [Fact]
    public void Constructor_WithZeroRates_IsAllowed()
    {
        var assumption = new GrowthAssumption(null, AccountType.Banking, 0m, 0m);

        assumption.AnnualGrowthRate.Should().Be(0m);
        assumption.AnnualInflationRate.Should().Be(0m);
    }

    [Fact]
    public void RealGrowthRate_ReturnsNominalMinusInflation()
    {
        var assumption = new GrowthAssumption(null, AccountType.Investment, 0.08m, 0.02m);

        assumption.RealGrowthRate.Should().Be(0.06m);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new GrowthAssumption(id, null, 0.07m, 0.02m);
        var b = new GrowthAssumption(id, null, 0.07m, 0.02m);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new GrowthAssumption(null, AccountType.Investment, 0.07m, 0.02m);
        var b = new GrowthAssumption(null, AccountType.Investment, 0.10m, 0.02m);

        a.Should().NotBe(b);
    }
}
