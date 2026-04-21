namespace Privestio.Contracts.Responses;

public record AccountUncategorizedCountResponse
{
    public Guid AccountId { get; init; }
    public int UncategorizedCount { get; init; }
}
