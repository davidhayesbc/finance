namespace Privestio.Contracts.Responses;

public record TagResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
