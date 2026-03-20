using Privestio.Domain.Services;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a provider or broker specific symbol for a canonical security.
/// </summary>
public class SecurityAlias : BaseEntity
{
    private SecurityAlias() { }

    internal SecurityAlias(
        Guid securityId,
        string symbol,
        string source,
        bool isPrimary,
        string? exchange
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        SecurityId = securityId;
        Symbol = SecuritySymbolMatcher.Normalize(symbol);
        Source = source.Trim();
        Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant();
        IsPrimary = isPrimary;
    }

    public Guid SecurityId { get; private set; }
    public Security? Security { get; set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string? Exchange { get; private set; }
    public bool IsPrimary { get; private set; }

    internal void UpdatePrimary(bool isPrimary)
    {
        if (IsPrimary == isPrimary)
            return;

        IsPrimary = isPrimary;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void UpdateDetails(string symbol, string source, string? exchange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Symbol = SecuritySymbolMatcher.Normalize(symbol);
        Source = source.Trim();
        Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }
}
