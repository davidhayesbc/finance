using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetBudgets;

public record GetBudgetsQuery(Guid UserId, int? Year = null, int? Month = null)
    : IRequest<IReadOnlyList<BudgetResponse>>;
