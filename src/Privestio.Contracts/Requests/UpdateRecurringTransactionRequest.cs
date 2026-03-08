namespace Privestio.Contracts.Requests;

public record UpdateRecurringTransactionRequest(
    string Description,
    decimal Amount,
    string TransactionType,
    string Frequency,
    DateTime StartDate,
    DateTime? EndDate = null,
    string Currency = "CAD",
    Guid? CategoryId = null,
    Guid? PayeeId = null,
    string? Notes = null
);
