using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.SuggestCategorizationRules;

public record SuggestCategorizationRulesCommand(
    Stream FileStream,
    string FileName,
    Guid AccountId,
    Guid UserId,
    Guid? MappingId,
    int MaxSuggestions = 8
) : IRequest<IReadOnlyList<RuleSuggestionResponse>>;
