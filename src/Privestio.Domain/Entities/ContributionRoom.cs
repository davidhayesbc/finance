using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Tracks contribution room for registered accounts (RRSP, TFSA, RESP, etc.)
/// with annual limit management and carry-forward.
/// </summary>
public class ContributionRoom : BaseEntity
{
    private ContributionRoom() { }

    public ContributionRoom(Guid accountId, int year, Money annualLimit, Money carryForwardRoom)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1900);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 2200);

        if (annualLimit.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(annualLimit),
                "Annual limit must be non-negative."
            );

        if (carryForwardRoom.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(carryForwardRoom),
                "Carry-forward room must be non-negative."
            );

        AccountId = accountId;
        Year = year;
        AnnualLimit = annualLimit;
        CarryForwardRoom = carryForwardRoom;
        ContributionsYtd = Money.Zero(annualLimit.CurrencyCode);
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public int Year { get; private set; }
    public Money AnnualLimit { get; private set; }
    public Money CarryForwardRoom { get; private set; }
    public Money ContributionsYtd { get; private set; }

    /// <summary>
    /// Remaining contribution room: CarryForward + AnnualLimit - ContributionsYtd.
    /// </summary>
    public Money RemainingRoom =>
        new(
            CarryForwardRoom.Amount + AnnualLimit.Amount - ContributionsYtd.Amount,
            AnnualLimit.CurrencyCode
        );

    public void RecordContribution(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Contribution must be positive.");

        ContributionsYtd = ContributionsYtd.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAnnualLimit(Money limit)
    {
        if (limit.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Annual limit must be non-negative."
            );

        AnnualLimit = limit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCarryForward(Money carryForward)
    {
        if (carryForward.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(carryForward),
                "Carry-forward room must be non-negative."
            );

        CarryForwardRoom = carryForward;
        UpdatedAt = DateTime.UtcNow;
    }
}
