using System.Text.Json;
using System.Text.Json.Serialization;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Rules;

/// <summary>
/// Evaluates categorization rules against transactions using JSON-based conditions.
/// Rules are evaluated in priority order; first match wins.
/// </summary>
public class CategorizationRuleEvaluator : IRuleEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public Task<RuleEvaluationResult> EvaluateAsync(
        Transaction transaction,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken cancellationToken = default
    )
    {
        var sortedRules = rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority);

        foreach (var rule in sortedRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchesConditions(transaction, rule.Conditions))
            {
                var action = ParseAction(rule.Actions);
                return Task.FromResult(new RuleEvaluationResult(true, rule, action));
            }
        }

        return Task.FromResult(new RuleEvaluationResult(false));
    }

    private static bool MatchesConditions(Transaction transaction, string conditionsJson)
    {
        var conditions = JsonSerializer.Deserialize<RuleConditions>(conditionsJson, JsonOptions);
        if (conditions is null)
            return false;

        // Description contains check (case-insensitive)
        if (
            conditions.DescriptionContains is not null
            && !transaction.Description.Contains(
                conditions.DescriptionContains,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return false;
        }

        // Amount range check
        if (
            conditions.MinAmount.HasValue
            && (double)transaction.Amount.Amount < conditions.MinAmount.Value
        )
        {
            return false;
        }

        if (
            conditions.MaxAmount.HasValue
            && (double)transaction.Amount.Amount > conditions.MaxAmount.Value
        )
        {
            return false;
        }

        return true;
    }

    private static RuleAction ParseAction(string actionsJson)
    {
        var parsed = JsonSerializer.Deserialize<RuleActionDto>(actionsJson, JsonOptions);
        if (parsed is null)
            return new RuleAction();

        var splitTemplates = parsed
            .SplitTemplates?.Select(s => new SplitTemplate(
                s.CategoryId,
                (decimal)s.Percentage,
                s.Notes
            ))
            .ToList();

        return new RuleAction(
            CategoryId: parsed.CategoryId,
            PayeeId: parsed.PayeeId,
            TagIds: parsed.TagIds,
            SplitTemplates: splitTemplates
        );
    }

    private sealed record RuleConditions
    {
        public string? DescriptionContains { get; init; }
        public double? MinAmount { get; init; }
        public double? MaxAmount { get; init; }
    }

    private sealed record RuleActionDto
    {
        public Guid? CategoryId { get; init; }
        public Guid? PayeeId { get; init; }
        public List<Guid>? TagIds { get; init; }
        public List<SplitTemplateDto>? SplitTemplates { get; init; }
    }

    private sealed record SplitTemplateDto
    {
        public Guid CategoryId { get; init; }
        public double Percentage { get; init; }
        public string? Notes { get; init; }
    }
}
