namespace Privestio.Contracts.Requests;

public record CreateReconciliationPeriodRequest(
    Guid AccountId,
    DateOnly StatementDate,
    decimal StatementBalanceAmount,
    string Currency = "CAD",
    string? Notes = null
);
