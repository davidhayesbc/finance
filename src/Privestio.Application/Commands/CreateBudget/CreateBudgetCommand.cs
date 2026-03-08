using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateBudget;

public record CreateBudgetCommand(
    Guid UserId,
    Guid CategoryId,
    int Year,
    int Month,
    decimal Amount,
    string Currency = "CAD",
    bool RolloverEnabled = false,
    string? Notes = null
) : IRequest<BudgetResponse>;
