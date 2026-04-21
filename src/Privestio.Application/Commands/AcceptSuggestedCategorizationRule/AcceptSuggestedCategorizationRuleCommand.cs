using MediatR;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AcceptSuggestedCategorizationRule;

public record AcceptSuggestedCategorizationRuleCommand(
    Guid AccountId,
    Guid UserId,
    string Name,
    int Priority,
    string Conditions,
    Guid CategoryId,
    bool IsEnabled = true,
    RuleApplyScope ApplyScope = RuleApplyScope.AllMatching
) : IRequest<AcceptRuleSuggestionResponse>;
