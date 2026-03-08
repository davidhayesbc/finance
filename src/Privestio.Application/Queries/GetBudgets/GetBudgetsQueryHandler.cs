using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetBudgets;

public class GetBudgetsQueryHandler
    : IRequestHandler<GetBudgetsQuery, IReadOnlyList<BudgetResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BudgetResponse>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken
    )
    {
        var budgets =
            request.Year.HasValue && request.Month.HasValue
                ? await _unitOfWork.Budgets.GetByUserIdAndPeriodAsync(
                    request.UserId,
                    request.Year.Value,
                    request.Month.Value,
                    cancellationToken
                )
                : await _unitOfWork.Budgets.GetByUserIdAsync(request.UserId, cancellationToken);

        return budgets.Select(BudgetMapper.ToResponse).ToList().AsReadOnly();
    }
}
