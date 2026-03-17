using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents an investment position for a single symbol within an account.
/// </summary>
public class Holding : BaseEntity
{
    private readonly List<Lot> _lots = [];

    private Holding() { }

    public Holding(
        Guid accountId,
        Guid securityId,
        string symbol,
        string securityName,
        decimal quantity,
        Money averageCostPerUnit,
        string? notes = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityName);
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        AccountId = accountId;
        SecurityId = securityId;
        Symbol = symbol.ToUpperInvariant().Trim();
        SecurityName = securityName.Trim();
        Quantity = quantity;
        AverageCostPerUnit = averageCostPerUnit;
        Notes = notes;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public Guid SecurityId { get; private set; }
    public Security? Security { get; set; }

    public string Symbol { get; private set; } = string.Empty;
    public string SecurityName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public Money AverageCostPerUnit { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<Lot> Lots => _lots.AsReadOnly();

    public void Update(decimal quantity, Money averageCostPerUnit, string? notes = null)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity = quantity;
        AverageCostPerUnit = averageCostPerUnit;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RenameSecurity(string securityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(securityName);
        SecurityName = securityName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RebindSecurity(Security security)
    {
        ArgumentNullException.ThrowIfNull(security);

        SecurityId = security.Id;
        Security = security;
        Symbol = security.DisplaySymbol;
        SecurityName = security.Name;
        UpdatedAt = DateTime.UtcNow;
    }
}
