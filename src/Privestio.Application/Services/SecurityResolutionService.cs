using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Services;

namespace Privestio.Application.Services;

public class SecurityResolutionService
{
    private static readonly HashSet<string> KnownExchangeSuffixes =
    [
        "TO",
        "V",
        "NE",
        "CN",
        "L",
        "AX",
        "HK",
        "PA",
        "DE",
        "SW",
        "NS",
        "BO",
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly Dictionary<string, Security> _resolvedSecurityCache = new(
        StringComparer.Ordinal
    );

    public SecurityResolutionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Security?> ResolveAsync(
        string symbol,
        CancellationToken cancellationToken = default
    ) => await _unitOfWork.Securities.GetByAnySymbolAsync(symbol, cancellationToken);

    public async Task<Security?> ResolveAsync(
        string symbol,
        string? source,
        string? exchange,
        IReadOnlyDictionary<SecurityIdentifierType, string>? identifiers,
        CancellationToken cancellationToken = default
    )
    {
        if (identifiers is not null)
        {
            foreach (var identifier in identifiers.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
            {
                var byIdentifier = await _unitOfWork.Securities.GetByIdentifierAsync(
                    identifier.Key,
                    identifier.Value,
                    cancellationToken
                );
                if (byIdentifier is not null)
                    return byIdentifier;
            }
        }

        var byExactAliasContext = await _unitOfWork.Securities.GetByAliasContextAsync(
            symbol,
            source,
            exchange,
            cancellationToken
        );
        if (byExactAliasContext is not null)
            return byExactAliasContext;

        if (!string.IsNullOrWhiteSpace(source))
        {
            var bySource = await _unitOfWork.Securities.GetByAliasContextAsync(
                symbol,
                source,
                null,
                cancellationToken
            );
            if (bySource is not null)
                return bySource;
        }

        if (!string.IsNullOrWhiteSpace(exchange))
        {
            var byExchange = await _unitOfWork.Securities.GetByAliasContextAsync(
                symbol,
                null,
                exchange,
                cancellationToken
            );
            if (byExchange is not null)
                return byExchange;
        }

        return await ResolveAsync(symbol, cancellationToken);
    }

    public async Task<Security> ResolveOrCreateAsync(
        string symbol,
        string? securityName,
        string currency,
        bool? isCashEquivalent = null,
        bool preferSymbolAsDisplay = true,
        string? source = null,
        string? exchange = null,
        IReadOnlyDictionary<SecurityIdentifierType, string>? identifiers = null,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (TryGetCachedSecurity(normalizedSymbol, source, exchange, out var cached))
        {
            ApplyUpdates(
                cached,
                normalizedSymbol,
                securityName,
                preferSymbolAsDisplay,
                isCashEquivalent,
                source,
                exchange,
                identifiers
            );
            CacheSecurity(cached, normalizedSymbol, source, exchange, identifiers);
            return cached;
        }

        var existing = await ResolveAsync(
            normalizedSymbol,
            source,
            exchange,
            identifiers,
            cancellationToken
        );
        if (existing is not null)
        {
            ApplyUpdates(
                existing,
                normalizedSymbol,
                securityName,
                preferSymbolAsDisplay,
                isCashEquivalent,
                source,
                exchange,
                identifiers
            );

            await _unitOfWork.Securities.UpdateAsync(existing, cancellationToken);
            CacheSecurity(existing, normalizedSymbol, source, exchange, identifiers);
            return existing;
        }

        var created = new Security(
            normalizedSymbol,
            normalizedSymbol,
            string.IsNullOrWhiteSpace(securityName) ? normalizedSymbol : securityName,
            normalizedCurrency,
            exchange,
            isCashEquivalent: isCashEquivalent.GetValueOrDefault()
                || IsCashEquivalentSymbol(normalizedSymbol)
        );

        if (!string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(exchange))
        {
            created.AddOrUpdateAlias(normalizedSymbol, source, true, exchange);
        }

        if (identifiers is not null)
        {
            foreach (var identifier in identifiers.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
            {
                created.AddOrUpdateIdentifier(identifier.Key, identifier.Value, true);
            }
        }

        await _unitOfWork.Securities.AddAsync(created, cancellationToken);
        CacheSecurity(created, normalizedSymbol, source, exchange, identifiers);
        return created;
    }

    private bool TryGetCachedSecurity(
        string normalizedSymbol,
        string? source,
        string? exchange,
        out Security security
    )
    {
        var key = BuildCacheKey(normalizedSymbol, source, exchange);
        return _resolvedSecurityCache.TryGetValue(key, out security!);
    }

    private void CacheSecurity(
        Security security,
        string normalizedSymbol,
        string? source,
        string? exchange,
        IReadOnlyDictionary<SecurityIdentifierType, string>? identifiers
    )
    {
        _resolvedSecurityCache[BuildCacheKey(normalizedSymbol, source, exchange)] = security;
        _resolvedSecurityCache[BuildCacheKey(security.CanonicalSymbol, source, exchange)] =
            security;
        _resolvedSecurityCache[BuildCacheKey(security.DisplaySymbol, source, exchange)] = security;

        foreach (var alias in security.Aliases)
        {
            _resolvedSecurityCache[BuildCacheKey(alias.Symbol, alias.Source, alias.Exchange)] =
                security;
        }

        if (identifiers is not null)
        {
            foreach (var identifier in identifiers.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
            {
                _resolvedSecurityCache[
                    $"ID|{identifier.Key}|{identifier.Value.Trim().ToUpperInvariant()}"
                ] = security;
            }
        }
    }

    private static string BuildCacheKey(string symbol, string? source, string? exchange)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        var normalizedExchange = string.IsNullOrWhiteSpace(exchange)
            ? string.Empty
            : exchange.Trim().ToUpperInvariant();
        return $"{symbol}|{normalizedSource}|{normalizedExchange}";
    }

    private static void ApplyUpdates(
        Security security,
        string normalizedSymbol,
        string? securityName,
        bool preferSymbolAsDisplay,
        bool? isCashEquivalent,
        string? source,
        string? exchange,
        IReadOnlyDictionary<SecurityIdentifierType, string>? identifiers
    )
    {
        if (!security.HasAlias(normalizedSymbol, source, exchange))
        {
            security.AddOrUpdateAlias(normalizedSymbol, source, false, exchange);
        }

        if (preferSymbolAsDisplay)
        {
            security.UpdateDisplaySymbol(normalizedSymbol);
        }

        if (!string.IsNullOrWhiteSpace(securityName))
        {
            security.Rename(securityName);
        }

        if (isCashEquivalent.GetValueOrDefault() || IsCashEquivalentSymbol(normalizedSymbol))
        {
            security.MarkCashEquivalent();
        }

        if (identifiers is not null)
        {
            foreach (var identifier in identifiers.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
            {
                if (!security.HasIdentifier(identifier.Key, identifier.Value))
                {
                    security.AddOrUpdateIdentifier(identifier.Key, identifier.Value, true);
                }
            }
        }
    }

    public string GetPreferredPriceLookupSymbol(Security security, string providerName)
    {
        ArgumentNullException.ThrowIfNull(security);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var providerAlias = security.GetPreferredSymbol(providerName);
        if (!string.Equals(providerAlias, security.DisplaySymbol, StringComparison.Ordinal))
            return providerAlias;

        return providerName switch
        {
            "YahooFinance" => FormatYahooFinanceSymbol(security),
            _ => security.DisplaySymbol,
        };
    }

    private static bool IsCashEquivalentSymbol(string symbol) =>
        SecuritySymbolMatcher
            .Normalize(symbol)
            .StartsWith("CASH", StringComparison.OrdinalIgnoreCase);

    private static string FormatYahooFinanceSymbol(Security security)
    {
        var normalized = SecuritySymbolMatcher.Normalize(security.DisplaySymbol);
        if (HasExchangeSuffix(normalized))
            return normalized;

        var dotIndex = normalized.LastIndexOf('.');
        var isCadSecurity = string.Equals(
            security.Currency,
            "CAD",
            StringComparison.OrdinalIgnoreCase
        );

        if (dotIndex > 0)
        {
            var suffix = normalized[(dotIndex + 1)..];
            var shareClassStyle = suffix.Length <= 2 && !KnownExchangeSuffixes.Contains(suffix);
            if (shareClassStyle)
            {
                var yahooBase = normalized.Replace('.', '-');
                return isCadSecurity ? $"{yahooBase}.TO" : yahooBase;
            }
        }

        return isCadSecurity ? $"{normalized}.TO" : normalized;
    }

    private static bool HasExchangeSuffix(string symbol)
    {
        var dotIndex = symbol.LastIndexOf('.');
        if (dotIndex <= 0 || dotIndex == symbol.Length - 1)
            return false;

        var suffix = symbol[(dotIndex + 1)..];
        return KnownExchangeSuffixes.Contains(suffix);
    }
}
