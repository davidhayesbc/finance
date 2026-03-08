namespace Privestio.Contracts.Responses;

public record NotificationResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAt { get; init; }
}
