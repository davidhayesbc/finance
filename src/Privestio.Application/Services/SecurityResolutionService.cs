using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
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
    private readonly Dictionary<string, Security> _resolvedSymbolCache = new(
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

    public async Task<Security> ResolveOrCreateAsync(
        string symbol,
        string? securityName,
        string currency,
        bool? isCashEquivalent = null,
        bool preferSymbolAsDisplay = true,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (TryGetCachedSecurity(normalizedSymbol, out var cached))
        {
            ApplyUpdates(
                cached,
                normalizedSymbol,
                securityName,
                preferSymbolAsDisplay,
                isCashEquivalent
            );
            CacheSecurity(cached, normalizedSymbol);
            return cached;
        }

        var existing = await ResolveAsync(normalizedSymbol, cancellationToken);
        if (existing is not null)
        {
            ApplyUpdates(
                existing,
                normalizedSymbol,
                securityName,
                preferSymbolAsDisplay,
                isCashEquivalent
            );

            await _unitOfWork.Securities.UpdateAsync(existing, cancellationToken);
            CacheSecurity(existing, normalizedSymbol);
            return existing;
        }

        var created = new Security(
            normalizedSymbol,
            normalizedSymbol,
            string.IsNullOrWhiteSpace(securityName) ? normalizedSymbol : securityName,
            normalizedCurrency,
            isCashEquivalent: isCashEquivalent.GetValueOrDefault()
                || IsCashEquivalentSymbol(normalizedSymbol)
        );

        await _unitOfWork.Securities.AddAsync(created, cancellationToken);
        CacheSecurity(created, normalizedSymbol);
        return created;
    }

    private bool TryGetCachedSecurity(string normalizedSymbol, out Security security)
    {
        return _resolvedSymbolCache.TryGetValue(normalizedSymbol, out security!);
    }

    private void CacheSecurity(Security security, string normalizedSymbol)
    {
        _resolvedSymbolCache[normalizedSymbol] = security;
        _resolvedSymbolCache[security.CanonicalSymbol] = security;
        _resolvedSymbolCache[security.DisplaySymbol] = security;

        foreach (var alias in security.Aliases)
        {
            _resolvedSymbolCache[alias.Symbol] = security;
        }
    }

    private static void ApplyUpdates(
        Security security,
        string normalizedSymbol,
        string? securityName,
        bool preferSymbolAsDisplay,
        bool? isCashEquivalent
    )
    {
        if (!security.HasAlias(normalizedSymbol))
        {
            security.AddOrUpdateAlias(normalizedSymbol, null);
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
