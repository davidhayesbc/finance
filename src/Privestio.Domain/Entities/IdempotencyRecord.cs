namespace Privestio.Domain.Entities;

public class IdempotencyRecord : BaseEntity
{
    private IdempotencyRecord() { }

    public IdempotencyRecord(string operationId, string response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        OperationId = operationId;
        ResponseData = response;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
    }

    public string OperationId { get; private set; } = string.Empty;
    public string ResponseData { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
}
