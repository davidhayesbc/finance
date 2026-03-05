using System.Text.Json;
using FluentAssertions;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Privestio.Infrastructure.Rules;
using Xunit;

namespace Privestio.Infrastructure.Tests.Rules;

public class CategorizationRuleEvaluatorTests
{
    private readonly CategorizationRuleEvaluator _evaluator = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task EvaluateAsync_DescriptionContainsMatch_ReturnsMatch()
    {
        var transaction = CreateTransaction(-42.99m, "GROCERY STORE #123");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Groceries",
                new { DescriptionContains = "GROCERY" },
                new { CategoryId = Guid.NewGuid() }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.IsMatch.Should().BeTrue();
        result.MatchedRule!.Name.Should().Be("Groceries");
        result.Action!.CategoryId.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateAsync_DescriptionContainsMatch_CaseInsensitive()
    {
        var transaction = CreateTransaction(-10.00m, "coffee shop visit");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Coffee",
                new { DescriptionContains = "COFFEE SHOP" },
                new { CategoryId = Guid.NewGuid() }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.IsMatch.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NoMatch_ReturnsNoMatch()
    {
        var transaction = CreateTransaction(-42.99m, "RESTAURANT");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Groceries",
                new { DescriptionContains = "GROCERY" },
                new { CategoryId = Guid.NewGuid() }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.IsMatch.Should().BeFalse();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_FirstMatchByPriority_Wins()
    {
        var transaction = CreateTransaction(-42.99m, "GROCERY STORE ORGANIC");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Organic",
                new { DescriptionContains = "ORGANIC" },
                new { CategoryId = Guid.NewGuid() },
                priority: 1
            ),
            CreateRule(
                "Groceries",
                new { DescriptionContains = "GROCERY" },
                new { CategoryId = Guid.NewGuid() },
                priority: 2
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.MatchedRule!.Name.Should().Be("Organic");
    }

    [Fact]
    public async Task EvaluateAsync_AmountRange_MatchesWithinRange()
    {
        var transaction = CreateTransaction(-150.00m, "POWER COMPANY");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Utilities",
                new
                {
                    DescriptionContains = "POWER",
                    MinAmount = -200.0,
                    MaxAmount = -50.0,
                },
                new { CategoryId = Guid.NewGuid() }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.IsMatch.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AmountOutOfRange_NoMatch()
    {
        var transaction = CreateTransaction(-500.00m, "POWER COMPANY");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Small Utilities",
                new
                {
                    DescriptionContains = "POWER",
                    MinAmount = -200.0,
                    MaxAmount = -50.0,
                },
                new { CategoryId = Guid.NewGuid() }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_ActionWithPayeeAndTags_ReturnsFullAction()
    {
        var categoryId = Guid.NewGuid();
        var payeeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var transaction = CreateTransaction(-42.99m, "GROCERY STORE");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Groceries Full",
                new { DescriptionContains = "GROCERY" },
                new
                {
                    CategoryId = categoryId,
                    PayeeId = payeeId,
                    TagIds = new[] { tagId },
                }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.Action!.CategoryId.Should().Be(categoryId);
        result.Action.PayeeId.Should().Be(payeeId);
        result.Action.TagIds.Should().Contain(tagId);
    }

    [Fact]
    public async Task EvaluateAsync_DisabledRule_Skipped()
    {
        var transaction = CreateTransaction(-42.99m, "GROCERY STORE");
        var rule = CreateRule(
            "Groceries",
            new { DescriptionContains = "GROCERY" },
            new { CategoryId = Guid.NewGuid() }
        );
        rule.Disable();

        var result = await _evaluator.EvaluateAsync(transaction, [rule]);

        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyRules_ReturnsNoMatch()
    {
        var transaction = CreateTransaction(-42.99m, "GROCERY STORE");

        var result = await _evaluator.EvaluateAsync(transaction, []);

        result.IsMatch.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_SplitTemplates_ReturnedInAction()
    {
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();

        var transaction = CreateTransaction(-100.00m, "COSTCO WHOLESALE");
        var rules = new List<CategorizationRule>
        {
            CreateRule(
                "Costco Split",
                new { DescriptionContains = "COSTCO" },
                new
                {
                    SplitTemplates = new[]
                    {
                        new
                        {
                            CategoryId = catId1,
                            Percentage = 60.0,
                            Notes = "Groceries",
                        },
                        new
                        {
                            CategoryId = catId2,
                            Percentage = 40.0,
                            Notes = "Household",
                        },
                    },
                }
            ),
        };

        var result = await _evaluator.EvaluateAsync(transaction, rules);

        result.Action!.SplitTemplates.Should().HaveCount(2);
        result.Action.SplitTemplates![0].Percentage.Should().Be(60.0m);
        result.Action.SplitTemplates![1].Percentage.Should().Be(40.0m);
    }

    private Transaction CreateTransaction(decimal amount, string description)
    {
        var type = amount >= 0 ? TransactionType.Credit : TransactionType.Debit;
        return new Transaction(
            Guid.NewGuid(),
            DateTime.UtcNow,
            new Money(amount),
            description,
            type
        );
    }

    private CategorizationRule CreateRule(
        string name,
        object conditions,
        object actions,
        int priority = 1
    )
    {
        return new CategorizationRule(
            name,
            priority,
            JsonSerializer.Serialize(conditions),
            JsonSerializer.Serialize(actions),
            _userId
        );
    }
}
