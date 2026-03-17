namespace Privestio.Contracts.Responses;

public record HistoricalPriceSyncResponse
{
    public string Provider { get; init; } = string.Empty;
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int SecuritiesProcessed { get; init; }
    public int QuotesFetched { get; init; }
    public int QuotesInserted { get; init; }
    public int QuotesSkipped { get; init; }
}
