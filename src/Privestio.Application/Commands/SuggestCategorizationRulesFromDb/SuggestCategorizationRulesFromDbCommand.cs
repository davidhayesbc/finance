using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.SuggestCategorizationRulesFromDb;

public record SuggestCategorizationRulesFromDbCommand(
    Guid AccountId,
    Guid UserId,
    int MaxSuggestions = 8
) : IRequest<IReadOnlyList<RuleSuggestionResponse>>;
