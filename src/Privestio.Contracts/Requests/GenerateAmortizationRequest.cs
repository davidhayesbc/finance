namespace Privestio.Contracts.Requests;

public record GenerateAmortizationRequest(
    Guid AccountId,
    decimal PrincipalAmount,
    decimal AnnualInterestRate,
    int TermMonths,
    decimal MonthlyPaymentAmount,
    DateOnly StartDate,
    string Currency = "CAD"
);
