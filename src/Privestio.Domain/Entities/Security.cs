using Privestio.Domain.Enums;
using Privestio.Domain.Services;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a canonical security identity independent of provider-specific symbols.
/// </summary>
public class Security : BaseEntity
{
    private readonly List<SecurityAlias> _aliases = [];
    private readonly List<SecurityIdentifier> _identifiers = [];

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
    public IReadOnlyCollection<SecurityIdentifier> Identifiers => _identifiers.AsReadOnly();

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

    public void UpdateCurrency(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Currency = currency.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateExchange(string? exchange)
    {
        Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCashEquivalent(bool isCashEquivalent)
    {
        IsCashEquivalent = isCashEquivalent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCashEquivalent()
    {
        IsCashEquivalent = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public SecurityAlias AddOrUpdateAlias(
        string symbol,
        string? source,
        bool isPrimary = false,
        string? exchange = null
    )
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = NormalizeSource(source);
        var normalizedExchange = NormalizeExchange(exchange);

        var existing = _aliases.FirstOrDefault(a =>
            a.Symbol == normalizedSymbol
            && string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
            && string.Equals(a.Exchange, normalizedExchange, StringComparison.Ordinal)
        );

        if (existing is not null)
        {
            if (isPrimary)
            {
                ClearPrimary(normalizedSource, normalizedExchange);
            }

            existing.UpdatePrimary(isPrimary);
            UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        if (isPrimary)
        {
            ClearPrimary(normalizedSource, normalizedExchange);
        }

        var alias = new SecurityAlias(
            Id,
            normalizedSymbol,
            normalizedSource,
            isPrimary,
            normalizedExchange
        );
        _aliases.Add(alias);
        UpdatedAt = DateTime.UtcNow;
        return alias;
    }

    public bool HasAlias(string symbol, string? source = null, string? exchange = null)
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = NormalizeSource(source);
        var normalizedExchange = NormalizeExchange(exchange);

        return _aliases.Any(a =>
            a.Symbol == normalizedSymbol
            && (
                normalizedSource is null
                || string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
            )
            && (
                normalizedExchange is null
                || string.Equals(a.Exchange, normalizedExchange, StringComparison.Ordinal)
            )
        );
    }

    public string GetPreferredSymbol(string? source = null, string? exchange = null)
    {
        var normalizedSource = NormalizeSource(source);
        var normalizedExchange = NormalizeExchange(exchange);

        if (normalizedExchange is not null)
        {
            var sourceExchangePrimary = _aliases.FirstOrDefault(a =>
                a.IsPrimary
                && string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
                && string.Equals(a.Exchange, normalizedExchange, StringComparison.Ordinal)
            );
            if (sourceExchangePrimary is not null)
                return sourceExchangePrimary.Symbol;

            var sourceExchangeAlias = _aliases.FirstOrDefault(a =>
                string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
                && string.Equals(a.Exchange, normalizedExchange, StringComparison.Ordinal)
            );
            if (sourceExchangeAlias is not null)
                return sourceExchangeAlias.Symbol;
        }

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

    public SecurityAlias UpdateAlias(
        Guid aliasId,
        string symbol,
        string? source,
        string? exchange,
        bool isPrimary
    )
    {
        var alias = _aliases.FirstOrDefault(a => a.Id == aliasId);
        if (alias is null)
            throw new InvalidOperationException("Alias not found.");

        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = NormalizeSource(source);
        var normalizedExchange = NormalizeExchange(exchange);

        var isDisplayAlias = alias.Source is null && alias.Symbol == DisplaySymbol;
        if (
            isDisplayAlias
            && (
                !string.Equals(normalizedSymbol, DisplaySymbol, StringComparison.Ordinal)
                || normalizedSource is not null
                || normalizedExchange is not null
            )
        )
        {
            throw new InvalidOperationException(
                "Display alias details cannot be changed. Update display symbol from security details instead."
            );
        }

        var duplicateExists = _aliases.Any(a =>
            a.Id != aliasId
            && a.Symbol == normalizedSymbol
            && string.Equals(a.Source, normalizedSource, StringComparison.Ordinal)
            && string.Equals(a.Exchange, normalizedExchange, StringComparison.Ordinal)
        );
        if (duplicateExists)
            throw new InvalidOperationException(
                "An alias with the same symbol/source/exchange already exists."
            );

        alias.UpdateDetails(normalizedSymbol, normalizedSource, normalizedExchange);

        if (isPrimary)
        {
            ClearPrimary(normalizedSource, normalizedExchange);
            alias.UpdatePrimary(true);
        }
        else
        {
            alias.UpdatePrimary(false);
        }

        UpdatedAt = DateTime.UtcNow;
        return alias;
    }

    public SecurityIdentifier AddOrUpdateIdentifier(
        SecurityIdentifierType identifierType,
        string value,
        bool isPrimary = false
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = NormalizeIdentifier(value);

        var existing = _identifiers.FirstOrDefault(i =>
            i.IdentifierType == identifierType
            && string.Equals(i.Value, normalized, StringComparison.Ordinal)
        );

        if (existing is not null)
        {
            if (isPrimary)
            {
                ClearIdentifierPrimary(identifierType);
            }

            existing.UpdatePrimary(isPrimary);
            UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        if (isPrimary)
        {
            ClearIdentifierPrimary(identifierType);
        }

        var created = new SecurityIdentifier(Id, identifierType, normalized, isPrimary);
        _identifiers.Add(created);
        UpdatedAt = DateTime.UtcNow;
        return created;
    }

    public bool HasIdentifier(SecurityIdentifierType identifierType, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = NormalizeIdentifier(value);

        return _identifiers.Any(i =>
            i.IdentifierType == identifierType
            && string.Equals(i.Value, normalized, StringComparison.Ordinal)
        );
    }

    public bool RemoveIdentifier(Guid identifierId)
    {
        var identifier = _identifiers.FirstOrDefault(i => i.Id == identifierId);
        if (identifier is null)
            return false;

        _identifiers.Remove(identifier);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private void ClearPrimary(string? source, string? exchange)
    {
        foreach (
            var alias in _aliases.Where(a =>
                string.Equals(a.Source, source, StringComparison.Ordinal)
                && string.Equals(a.Exchange, exchange, StringComparison.Ordinal)
            )
        )
        {
            alias.UpdatePrimary(false);
        }
    }

    private void ClearIdentifierPrimary(SecurityIdentifierType identifierType)
    {
        foreach (var identifier in _identifiers.Where(i => i.IdentifierType == identifierType))
        {
            identifier.UpdatePrimary(false);
        }
    }

    private static string? NormalizeSource(string? source) =>
        string.IsNullOrWhiteSpace(source) ? null : source.Trim();

    private static string? NormalizeExchange(string? exchange) =>
        string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant();

    private static string NormalizeIdentifier(string value) => value.Trim().ToUpperInvariant();
}
