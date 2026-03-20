namespace Privestio.Contracts.Requests;

public record SetPricingProviderOrderRequest
{
    public List<string>? ProviderOrder { get; init; }
}
