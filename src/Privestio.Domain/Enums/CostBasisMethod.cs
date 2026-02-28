namespace Privestio.Domain.Enums;

/// <summary>
/// Cost basis calculation method for investment lots.
/// </summary>
public enum CostBasisMethod
{
    AverageCost = 1,
    FirstInFirstOut = 2,
    SpecificLot = 3,
}
