using MediatR;

namespace Privestio.Application.Commands.DeleteBudget;

public record DeleteBudgetCommand(Guid BudgetId, Guid UserId) : IRequest;
