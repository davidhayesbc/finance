using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Commands.AcceptSuggestedCategorizationRule;

public class AcceptSuggestedCategorizationRuleCommandHandler
    : IRequestHandler<AcceptSuggestedCategorizationRuleCommand, AcceptRuleSuggestionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuleEvaluator _ruleEvaluator;

    public AcceptSuggestedCategorizationRuleCommandHandler(
        IUnitOfWork unitOfWork,
        IRuleEvaluator ruleEvaluator
    )
    {
        _unitOfWork = unitOfWork;
        _ruleEvaluator = ruleEvaluator;
    }

    public async Task<AcceptRuleSuggestionResponse> Handle(
        AcceptSuggestedCategorizationRuleCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot apply a rule to an account owned by another user."
            );

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException("Category not found.");

        if (!category.IsSystem && category.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot use a category owned by another user."
            );

        var actionsJson = JsonSerializer.Serialize(new RuleActions(request.CategoryId));

        var rule = new CategorizationRule(
            request.Name,
            request.Priority,
            request.Conditions,
            actionsJson,
            request.UserId
        );

        if (!request.IsEnabled)
            rule.Disable();

        await _unitOfWork.CategorizationRules.AddAsync(rule, cancellationToken);

        var transactions = await _unitOfWork.Transactions.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );

        var appliedCount = 0;
        foreach (var transaction in transactions)
        {
            if (
                request.ApplyScope == RuleApplyScope.UncategorizedOnly
                && transaction.CategoryId.HasValue
            )
                continue;

            var evaluation = await _ruleEvaluator.EvaluateAsync(
                transaction,
                [rule],
                cancellationToken
            );

            if (!evaluation.IsMatch || evaluation.Action?.CategoryId is null)
                continue;

            transaction.CategoryId = evaluation.Action.CategoryId;
            await _unitOfWork.Transactions.UpdateAsync(transaction, cancellationToken);
            appliedCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptRuleSuggestionResponse
        {
            Rule = new CategorizationRuleResponse
            {
                Id = rule.Id,
                Name = rule.Name,
                Priority = rule.Priority,
                Conditions = rule.Conditions,
                Actions = rule.Actions,
                IsEnabled = rule.IsEnabled,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt,
            },
            AppliedTransactionCount = appliedCount,
        };
    }

    private sealed record RuleActions(Guid CategoryId);
}
