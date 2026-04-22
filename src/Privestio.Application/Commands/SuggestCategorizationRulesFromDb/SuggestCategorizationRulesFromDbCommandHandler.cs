using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Commands.SuggestCategorizationRulesFromDb;

public class SuggestCategorizationRulesFromDbCommandHandler
    : IRequestHandler<SuggestCategorizationRulesFromDbCommand, IReadOnlyList<RuleSuggestionResponse>>
{
    private const int MaxRowsForPrompt = 60;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IOllamaRuleSuggestionService _ollamaService;

    public SuggestCategorizationRulesFromDbCommandHandler(
        IUnitOfWork unitOfWork,
        IOllamaRuleSuggestionService ollamaService
    )
    {
        _unitOfWork = unitOfWork;
        _ollamaService = ollamaService;
    }

    public async Task<IReadOnlyList<RuleSuggestionResponse>> Handle(
        SuggestCategorizationRulesFromDbCommand request,
        CancellationToken cancellationToken
    )
    {
        var effectiveMaxSuggestions = Math.Max(1, Math.Min(request.MaxSuggestions, 4));

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot generate suggestions for an account owned by another user."
            );

        var transactions = await _unitOfWork.Transactions.GetUncategorizedByAccountIdAsync(
            request.AccountId,
            Math.Min(MaxRowsForPrompt, effectiveMaxSuggestions * 4),
            cancellationToken
        );

        var rows = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Description))
            .Select(t => new RuleSuggestionInputRow(t.Description, t.Amount.Amount))
            .ToList();

        if (rows.Count == 0)
            return [];

        var promptRows = rows
            .GroupBy(r =>
                new RuleSuggestionGroupKey(
                    NormalizeDescriptionForGrouping(r.Description),
                    decimal.Round(r.Amount, 2, MidpointRounding.ToEven)
                )
            )
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .Take(MaxRowsForPrompt)
            .ToList();

        var drafts = await _ollamaService.SuggestRulesAsync(
            promptRows,
            effectiveMaxSuggestions,
            cancellationToken
        );

        var uniqueDrafts = drafts
            .Where(d =>
                !string.IsNullOrWhiteSpace(d.Name)
                && !string.IsNullOrWhiteSpace(d.DescriptionContains)
                && !string.IsNullOrWhiteSpace(d.SuggestedCategoryName)
            )
            .DistinctBy(d => d.DescriptionContains.Trim().ToUpperInvariant())
            .Take(effectiveMaxSuggestions)
            .ToList();

        var suggestions = new List<RuleSuggestionResponse>(uniqueDrafts.Count);
        foreach (var draft in uniqueDrafts)
        {
            var conditions = new RuleConditions(
                draft.DescriptionContains.Trim(),
                draft.MinAmount,
                draft.MaxAmount
            );
            var conditionsJson = JsonSerializer.Serialize(conditions);
            var matchCount = CountMatches(rows, conditions);

            if (matchCount == 0)
                continue;

            var matchRate = Math.Round((decimal)matchCount / rows.Count, 4);
            suggestions.Add(
                new RuleSuggestionResponse
                {
                    Name = draft.Name.Trim(),
                    Priority = 200 + (suggestions.Count * 10),
                    Conditions = conditionsJson,
                    SuggestedCategoryName = draft.SuggestedCategoryName.Trim(),
                    Rationale = draft.Rationale.Trim(),
                    MatchCount = matchCount,
                    MatchRate = matchRate,
                }
            );
        }

        return suggestions;
    }

    private static int CountMatches(
        IReadOnlyCollection<RuleSuggestionInputRow> rows,
        RuleConditions conditions
    )
    {
        var count = 0;
        foreach (var row in rows)
        {
            if (
                !row.Description.Contains(
                    conditions.DescriptionContains,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                continue;

            if (conditions.MinAmount.HasValue && row.Amount < conditions.MinAmount.Value)
                continue;

            if (conditions.MaxAmount.HasValue && row.Amount > conditions.MaxAmount.Value)
                continue;

            count++;
        }
        return count;
    }

    private static string NormalizeDescriptionForGrouping(string description) =>
        string.Join(
            ' ',
            description
                .Trim()
                .ToUpperInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );

    private sealed record RuleConditions(
        string DescriptionContains,
        decimal? MinAmount,
        decimal? MaxAmount
    );

    private sealed record RuleSuggestionGroupKey(string NormalizedDescription, decimal RoundedAmount);
}
