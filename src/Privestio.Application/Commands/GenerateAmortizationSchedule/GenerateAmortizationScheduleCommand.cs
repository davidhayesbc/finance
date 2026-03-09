using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.GenerateAmortizationSchedule;

public record GenerateAmortizationScheduleCommand(
    Guid UserId,
    Guid AccountId,
    decimal PrincipalAmount,
    decimal AnnualInterestRate,
    int TermMonths,
    decimal MonthlyPaymentAmount,
    DateOnly StartDate,
    string Currency
) : IRequest<AmortizationScheduleResponse>;
