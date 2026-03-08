namespace Privestio.Contracts.Requests;

public record CreateBudgetRequest(
    Guid CategoryId,
    int Year,
    int Month,
    decimal Amount,
    string Currency = "CAD",
    bool RolloverEnabled = false,
    string? Notes = null
);
