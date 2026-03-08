using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateBudget;

public record UpdateBudgetCommand(
    Guid BudgetId,
    Guid UserId,
    decimal Amount,
    string Currency = "CAD",
    bool RolloverEnabled = false,
    string? Notes = null
) : IRequest<BudgetResponse>;
