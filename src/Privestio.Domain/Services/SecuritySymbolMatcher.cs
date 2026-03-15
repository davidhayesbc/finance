namespace Privestio.Domain.Services;

/// <summary>
/// Provides deterministic symbol normalization and alias candidates for
/// cross-provider/broker ticker mismatches (for example XEQT vs XEQT.TO).
/// </summary>
public static class SecuritySymbolMatcher
{
    private const string PrimaryCanadianExchangeSuffix = ".TO";

    public static string Normalize(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.ToUpperInvariant().Trim();
    }

    public static IReadOnlyList<string> GetLookupCandidates(string symbol)
    {
        var normalized = Normalize(symbol);
        var candidates = new List<string> { normalized };

        if (TryGetBaseSymbol(normalized, out var baseSymbol))
        {
            candidates.Add(baseSymbol);
        }
        else
        {
            candidates.Add($"{normalized}{PrimaryCanadianExchangeSuffix}");
        }

        return candidates;
    }

    private static bool TryGetBaseSymbol(string normalizedSymbol, out string baseSymbol)
    {
        baseSymbol = string.Empty;
        var dotIndex = normalizedSymbol.LastIndexOf('.');
        if (dotIndex <= 0)
            return false;

        baseSymbol = normalizedSymbol[..dotIndex];
        return !string.IsNullOrWhiteSpace(baseSymbol);
    }
}
