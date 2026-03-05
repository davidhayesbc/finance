namespace Privestio.Contracts.Responses;

public record PayeeResponse
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public Guid? DefaultCategoryId { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}
