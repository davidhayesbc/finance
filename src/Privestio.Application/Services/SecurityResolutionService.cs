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

        var existing = await ResolveAsync(normalizedSymbol, cancellationToken);
        if (existing is not null)
        {
            if (!existing.HasAlias(normalizedSymbol))
            {
                existing.AddOrUpdateAlias(normalizedSymbol, null);
            }

            if (preferSymbolAsDisplay)
            {
                existing.UpdateDisplaySymbol(normalizedSymbol);
            }

            if (!string.IsNullOrWhiteSpace(securityName))
            {
                existing.Rename(securityName);
            }

            if (isCashEquivalent.GetValueOrDefault() || IsCashEquivalentSymbol(normalizedSymbol))
            {
                existing.MarkCashEquivalent();
            }

            await _unitOfWork.Securities.UpdateAsync(existing, cancellationToken);
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
        return created;
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
