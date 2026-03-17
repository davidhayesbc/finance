namespace Privestio.Domain.Services;

/// <summary>
/// Provides deterministic symbol normalization helpers for security symbols.
/// </summary>
public static class SecuritySymbolMatcher
{
    public static string Normalize(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.ToUpperInvariant().Trim();
    }
}
