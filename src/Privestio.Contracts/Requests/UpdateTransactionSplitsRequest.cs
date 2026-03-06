namespace Privestio.Contracts.Requests;

public record UpdateTransactionSplitsRequest
{
    public IReadOnlyList<AddTransactionSplitRequest> Splits { get; init; } = [];
}
