using Privestio.Domain.Services;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a canonical security identity independent of provider-specific symbols.
/// </summary>
public class Security : BaseEntity
{
    private readonly List<SecurityAlias> _aliases = [];

    private Security() { }

    public Security(
        string canonicalSymbol,
        string displaySymbol,
        string name,
        string currency,
        string? exchange = null,
        bool isCashEquivalent = false
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(displaySymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        CanonicalSymbol = SecuritySymbolMatcher.Normalize(canonicalSymbol);
        DisplaySymbol = SecuritySymbolMatcher.Normalize(displaySymbol);
        Name = name.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant();
        IsCashEquivalent = isCashEquivalent;

        AddOrUpdateAlias(DisplaySymbol, null, true);
    }

    public string CanonicalSymbol { get; private set; } = string.Empty;
    public string DisplaySymbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public string? Exchange { get; private set; }
    public bool IsCashEquivalent { get; private set; }

    public IReadOnlyCollection<SecurityAlias> Aliases => _aliases.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplaySymbol(string displaySymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displaySymbol);
        DisplaySymbol = SecuritySymbolMatcher.Normalize(displaySymbol);
        AddOrUpdateAlias(DisplaySymbol, null, true);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCashEquivalent()
    {
        IsCashEquivalent = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public SecurityAlias AddOrUpdateAlias(string symbol, string? source, bool isPrimary = false)
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = NormalizeSource(source);

        var existing = _aliases.FirstOrDefault(a =>
            a.Symbol == normalizedSymbol
            && string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
        );

        if (existing is not null)
        {
            if (isPrimary)
            {
                ClearPrimary(normalizedSource);
            }

            existing.UpdatePrimary(isPrimary);
            UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        if (isPrimary)
        {
            ClearPrimary(normalizedSource);
        }

        var alias = new SecurityAlias(Id, normalizedSymbol, normalizedSource, isPrimary);
        _aliases.Add(alias);
        UpdatedAt = DateTime.UtcNow;
        return alias;
    }

    public bool HasAlias(string symbol, string? source = null)
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = NormalizeSource(source);

        return _aliases.Any(a =>
            a.Symbol == normalizedSymbol
            && (
                normalizedSource is null
                || string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
            )
        );
    }

    public string GetPreferredSymbol(string? source = null)
    {
        var normalizedSource = NormalizeSource(source);

        var providerPrimary = _aliases.FirstOrDefault(a =>
            a.IsPrimary && string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
        );
        if (providerPrimary is not null)
            return providerPrimary.Symbol;

        var providerAlias = _aliases.FirstOrDefault(a =>
            string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
        );
        if (providerAlias is not null)
            return providerAlias.Symbol;

        return DisplaySymbol;
    }

    public bool RemoveAlias(Guid aliasId)
    {
        var alias = _aliases.FirstOrDefault(a => a.Id == aliasId);
        if (alias is null)
            return false;

        // Keep canonical display mapping safe so every security remains resolvable.
        if (alias.Source is null && alias.Symbol == DisplaySymbol)
            return false;

        _aliases.Remove(alias);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private void ClearPrimary(string? source)
    {
        foreach (
            var alias in _aliases.Where(a =>
                string.Equals(a.Source, source, StringComparison.Ordinal)
            )
        )
        {
            alias.UpdatePrimary(false);
        }
    }

    private static string? NormalizeSource(string? source) =>
        string.IsNullOrWhiteSpace(source) ? null : source.Trim();
}
