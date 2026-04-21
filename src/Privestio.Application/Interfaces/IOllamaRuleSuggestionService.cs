namespace Privestio.Application.Interfaces;

public interface IOllamaRuleSuggestionService
{
    Task<IReadOnlyList<RuleSuggestionDraft>> SuggestRulesAsync(
        IReadOnlyList<RuleSuggestionInputRow> rows,
        int maxSuggestions,
        CancellationToken cancellationToken = default
    );
}

public sealed record RuleSuggestionInputRow(string Description, decimal Amount);

public sealed record RuleSuggestionDraft(
    string Name,
    string DescriptionContains,
    decimal? MinAmount,
    decimal? MaxAmount,
    string SuggestedCategoryName,
    string Rationale
);
