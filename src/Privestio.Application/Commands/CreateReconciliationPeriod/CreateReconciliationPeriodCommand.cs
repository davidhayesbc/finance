using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateReconciliationPeriod;

public record CreateReconciliationPeriodCommand(
    Guid UserId,
    Guid AccountId,
    DateOnly StatementDate,
    decimal StatementBalanceAmount,
    string Currency,
    string? Notes
) : IRequest<ReconciliationPeriodResponse>;
