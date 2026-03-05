using Privestio.Domain.Entities;

namespace Privestio.Domain.Interfaces;

/// <summary>
/// Evaluates categorization rules against transactions.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates all enabled rules against the given transaction and applies matching actions.
    /// Returns the rule that matched, or null if none matched.
    /// </summary>
    Task<RuleEvaluationResult> EvaluateAsync(
        Transaction transaction,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// The result of evaluating categorization rules against a transaction.
/// </summary>
public record RuleEvaluationResult(
    bool IsMatch,
    CategorizationRule? MatchedRule = null,
    RuleAction? Action = null
);

/// <summary>
/// The action to take when a rule matches.
/// </summary>
public record RuleAction(
    Guid? CategoryId = null,
    Guid? PayeeId = null,
    IReadOnlyList<Guid>? TagIds = null,
    IReadOnlyList<SplitTemplate>? SplitTemplates = null
);

/// <summary>
/// Template for auto-splitting a transaction when a rule fires (Task 2.14).
/// </summary>
public record SplitTemplate(Guid CategoryId, decimal Percentage, string? Notes = null);
