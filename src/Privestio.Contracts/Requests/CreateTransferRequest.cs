namespace Privestio.Contracts.Requests;

public record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    DateTime Date,
    string? Notes = null
);
