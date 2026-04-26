using System.Text.Json;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Commands.SuggestCategorizationRulesFromDb;

public class SuggestCategorizationRulesFromDbCommandHandler
    : IRequestHandler<SuggestCategorizationRulesFromDbCommand, IReadOnlyList<RuleSuggestionResponse>>
{
    private const int MaxRowsForPrompt = 120;
    private const int MinRowsForAiSample = 120;
    private const int RowsPerRequestedSuggestion = 40;
    private const int MaxRowsForAiSample = 300;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IOllamaRuleSuggestionService _ollamaService;
    private readonly ILogger<SuggestCategorizationRulesFromDbCommandHandler> _logger;

    public SuggestCategorizationRulesFromDbCommandHandler(
        IUnitOfWork unitOfWork,
        IOllamaRuleSuggestionService ollamaService,
        ILogger<SuggestCategorizationRulesFromDbCommandHandler>? logger = null
    )
    {
        _unitOfWork = unitOfWork;
        _ollamaService = ollamaService;
        _logger = logger ?? NullLogger<SuggestCategorizationRulesFromDbCommandHandler>.Instance;
    }

    public async Task<IReadOnlyList<RuleSuggestionResponse>> Handle(
        SuggestCategorizationRulesFromDbCommand request,
        CancellationToken cancellationToken
    )
    {
        var overallStopwatch = Stopwatch.StartNew();
        var effectiveMaxSuggestions = Math.Max(1, Math.Min(request.MaxSuggestions, 4));
        var rowsToFetch = Math.Clamp(
            effectiveMaxSuggestions * RowsPerRequestedSuggestion,
            MinRowsForAiSample,
            MaxRowsForAiSample
        );

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot generate suggestions for an account owned by another user."
            );

        var transactions = await _unitOfWork.Transactions.GetUncategorizedByAccountIdAsync(
            request.AccountId,
            rowsToFetch,
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

        var ollamaStopwatch = Stopwatch.StartNew();
        var drafts = await _ollamaService.SuggestRulesAsync(
            promptRows,
            effectiveMaxSuggestions,
            cancellationToken
        );
        ollamaStopwatch.Stop();

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
            var matchSamples = BuildMatchSamples(rows, conditions);
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
                    MatchSamples = matchSamples,
                }
            );
        }

        _logger.LogInformation(
            "DB rule suggestion request completed. AccountId={AccountId}, RequestedMaxSuggestions={RequestedMaxSuggestions}, EffectiveMaxSuggestions={EffectiveMaxSuggestions}, RowsRequested={RowsRequested}, RowsFetched={RowsFetched}, PromptRows={PromptRows}, Drafts={Drafts}, Suggestions={Suggestions}, OllamaMs={OllamaMs}, ElapsedMs={ElapsedMs}",
            request.AccountId,
            request.MaxSuggestions,
            effectiveMaxSuggestions,
            rowsToFetch,
            transactions.Count,
            promptRows.Count,
            drafts.Count,
            suggestions.Count,
            ollamaStopwatch.ElapsedMilliseconds,
            overallStopwatch.ElapsedMilliseconds
        );

        return suggestions;
    }

    private static int CountMatches(
        IReadOnlyCollection<RuleSuggestionInputRow> rows,
        RuleConditions conditions
    )
    {
        return rows.Count(row => Matches(row, conditions));
    }

    private static IReadOnlyList<RuleSuggestionMatchSampleResponse> BuildMatchSamples(
        IReadOnlyCollection<RuleSuggestionInputRow> rows,
        RuleConditions conditions
    ) =>
        rows.Where(row => Matches(row, conditions))
            .GroupBy(row =>
                new RuleSuggestionGroupKey(
                    NormalizeDescriptionForGrouping(row.Description),
                    decimal.Round(row.Amount, 2, MidpointRounding.ToEven)
                )
            )
            .OrderByDescending(group => group.Count())
            .Select(group =>
                new RuleSuggestionMatchSampleResponse
                {
                    Description = group.First().Description,
                    Amount = decimal.Round(group.First().Amount, 2, MidpointRounding.ToEven),
                    Frequency = group.Count(),
                }
            )
            .Take(3)
            .ToList();

    private static bool Matches(RuleSuggestionInputRow row, RuleConditions conditions)
    {
        if (
            !row.Description.Contains(
                conditions.DescriptionContains,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return false;

        if (conditions.MinAmount.HasValue && row.Amount < conditions.MinAmount.Value)
            return false;

        if (conditions.MaxAmount.HasValue && row.Amount > conditions.MaxAmount.Value)
            return false;

        return true;
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
