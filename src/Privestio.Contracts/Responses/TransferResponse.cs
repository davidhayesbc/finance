namespace Privestio.Contracts.Responses;

public record TransferResponse
{
    public Guid SourceTransactionId { get; init; }
    public Guid DestinationTransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime Date { get; init; }
}
