namespace Privestio.Contracts.Requests;

public record CreateSinkingFundRequest(
    string Name,
    decimal TargetAmount,
    DateTime DueDate,
    string Currency = "CAD",
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Notes = null
);
