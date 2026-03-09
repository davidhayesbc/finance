namespace Privestio.Domain.Enums;

/// <summary>
/// Status of a reconciliation period.
/// </summary>
public enum ReconciliationStatus
{
    Open = 1,
    Balanced = 2,
    Locked = 3,
}
