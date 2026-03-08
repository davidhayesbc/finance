namespace Privestio.Contracts.Requests;

public record UpdateBudgetRequest(
    decimal Amount,
    string Currency = "CAD",
    bool RolloverEnabled = false,
    string? Notes = null
);
