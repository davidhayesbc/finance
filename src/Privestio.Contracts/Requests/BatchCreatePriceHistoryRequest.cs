namespace Privestio.Contracts.Requests;

public record BatchCreatePriceHistoryRequest
{
    public IReadOnlyList<CreatePriceHistoryRequest> Entries { get; init; } = [];
}
