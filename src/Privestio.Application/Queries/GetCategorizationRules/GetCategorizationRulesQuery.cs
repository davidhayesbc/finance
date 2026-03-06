using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCategorizationRules;

public record GetCategorizationRulesQuery(Guid UserId)
    : IRequest<IReadOnlyList<CategorizationRuleResponse>>;
