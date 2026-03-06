using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCategorizationRules;

public class GetCategorizationRulesQueryHandler
    : IRequestHandler<GetCategorizationRulesQuery, IReadOnlyList<CategorizationRuleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategorizationRulesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<CategorizationRuleResponse>> Handle(
        GetCategorizationRulesQuery request,
        CancellationToken cancellationToken
    )
    {
        var rules = await _unitOfWork.CategorizationRules.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return rules
            .Select(r => new CategorizationRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Priority = r.Priority,
                Conditions = r.Conditions,
                Actions = r.Actions,
                IsEnabled = r.IsEnabled,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToList();
    }
}
