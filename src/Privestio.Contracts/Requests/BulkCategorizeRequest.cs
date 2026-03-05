namespace Privestio.Contracts.Requests;

public record BulkCategorizeRequest(IReadOnlyList<Guid> TransactionIds, Guid CategoryId);
