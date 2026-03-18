using Privestio.Domain.Enums;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a global security identifier (CUSIP/ISIN/FIGI/SEDOL) for canonical matching.
/// </summary>
public class SecurityIdentifier : BaseEntity
{
    private SecurityIdentifier() { }

    internal SecurityIdentifier(Guid securityId, SecurityIdentifierType identifierType, string value, bool isPrimary)
    {
        if (securityId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(securityId));
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        SecurityId = securityId;
        IdentifierType = identifierType;
        Value = NormalizeValue(value);
        IsPrimary = isPrimary;
    }

    public Guid SecurityId { get; private set; }
    public Security? Security { get; set; }
    public SecurityIdentifierType IdentifierType { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }

    internal void UpdatePrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeValue(string value) => value.Trim().ToUpperInvariant();
}
