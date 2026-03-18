namespace Privestio.Contracts.Responses;

public record SecurityCatalogItemResponse
{
    public Guid Id { get; init; }
    public string CanonicalSymbol { get; init; } = string.Empty;
    public string DisplaySymbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string? Exchange { get; init; }
    public bool IsCashEquivalent { get; init; }
    public IReadOnlyList<SecurityAliasResponse> Aliases { get; init; } = [];
    public IReadOnlyList<SecurityIdentifierResponse> Identifiers { get; init; } = [];
    public decimal? LatestPrice { get; init; }
    public string? LatestPriceCurrency { get; init; }
    public DateOnly? LatestPriceAsOfDate { get; init; }
    public string? LatestPriceSource { get; init; }
    public string? LatestProviderSymbol { get; init; }
}
