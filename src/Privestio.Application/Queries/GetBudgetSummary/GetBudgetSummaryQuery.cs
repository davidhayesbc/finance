using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetBudgetSummary;

public record GetBudgetSummaryQuery(Guid UserId, int Year, int Month)
    : IRequest<IReadOnlyList<BudgetSummaryResponse>>;
