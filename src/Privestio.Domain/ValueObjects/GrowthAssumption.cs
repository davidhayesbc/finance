using Privestio.Domain.Enums;

namespace Privestio.Domain.ValueObjects;

/// <summary>
/// Represents a growth rate assumption for forecasting, scoped to a specific account or account type.
/// </summary>
public readonly record struct GrowthAssumption(
    Guid? AccountId,
    AccountType? AccountType,
    decimal AnnualGrowthRate,
    decimal AnnualInflationRate
)
{
    /// <summary>
    /// Real growth rate after inflation adjustment.
    /// </summary>
    public decimal RealGrowthRate => AnnualGrowthRate - AnnualInflationRate;
}
