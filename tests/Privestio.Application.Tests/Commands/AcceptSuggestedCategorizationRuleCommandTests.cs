using FluentAssertions;
using Moq;
using Privestio.Contracts.Requests;
using Privestio.Application.Commands.AcceptSuggestedCategorizationRule;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class AcceptSuggestedCategorizationRuleCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ICategorizationRuleRepository> _ruleRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IRuleEvaluator> _ruleEvaluator = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WithValidRuleAndCategory_CreatesRuleAndAppliesToMatchingTransactions()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var handler = CreateHandler(categoryId);

        var command = new AcceptSuggestedCategorizationRuleCommand(
            AccountId: _accountId,
            UserId: _userId,
            Name: "Fortis recurring utility payment",
            Priority: 210,
            Conditions: "{\"DescriptionContains\":\"FortisBC Energ\"}",
            CategoryId: categoryId,
            IsEnabled: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Rule.Name.Should().Be("Fortis recurring utility payment");
        result.AppliedTransactionCount.Should().Be(1);

        _ruleRepository.Verify(
            r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _transactionRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var handler = CreateHandler(categoryId, categoryExists: false);
        var command = new AcceptSuggestedCategorizationRuleCommand(
            AccountId: _accountId,
            UserId: _userId,
            Name: "Fortis recurring utility payment",
            Priority: 210,
            Conditions: "{\"DescriptionContains\":\"FortisBC Energ\"}",
            CategoryId: categoryId,
            IsEnabled: true
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_DoesNotRecategorizeAlreadyCategorizedTransactions()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategoryId = Guid.NewGuid();
        var handler = CreateHandler(categoryId, matchingTransactionCategoryId: existingCategoryId);
        _ruleEvaluator
            .Setup(e =>
                e.EvaluateAsync(
                    It.IsAny<Transaction>(),
                    It.IsAny<IReadOnlyList<CategorizationRule>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new RuleEvaluationResult(false));

        var command = new AcceptSuggestedCategorizationRuleCommand(
            AccountId: _accountId,
            UserId: _userId,
            Name: "Fortis recurring utility payment",
            Priority: 210,
            Conditions: "{\"DescriptionContains\":\"FortisBC Energ\"}",
            CategoryId: categoryId,
            IsEnabled: true,
            ApplyScope: RuleApplyScope.UncategorizedOnly
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.AppliedTransactionCount.Should().Be(0);
        _transactionRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_AllMatchingScope_RecategorizesAlreadyCategorizedTransactions()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategoryId = Guid.NewGuid();
        var handler = CreateHandler(categoryId, matchingTransactionCategoryId: existingCategoryId);

        var command = new AcceptSuggestedCategorizationRuleCommand(
            AccountId: _accountId,
            UserId: _userId,
            Name: "Fortis recurring utility payment",
            Priority: 210,
            Conditions: "{\"DescriptionContains\":\"FortisBC Energ\"}",
            CategoryId: categoryId,
            IsEnabled: true,
            ApplyScope: RuleApplyScope.AllMatching
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.AppliedTransactionCount.Should().Be(1);
        _transactionRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private AcceptSuggestedCategorizationRuleCommandHandler CreateHandler(
        Guid categoryId,
        bool categoryExists = true,
        Guid? matchingTransactionCategoryId = null
    )
    {
        var account = new Account(
            "Joint",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _userId
        );

        var category = new Category("Utilities", CategoryType.Expense, _userId);

        var matchingTransaction = new Transaction(
            _accountId,
            DateTime.UtcNow,
            new Money(63.55m),
            "EFT Withdrawal to FortisBC Energy",
            TransactionType.Debit
        )
        {
            CategoryId = matchingTransactionCategoryId,
        };

        var nonMatchingTransaction = new Transaction(
            _accountId,
            DateTime.UtcNow.AddDays(-2),
            new Money(18.20m),
            "PAYPAL",
            TransactionType.Debit
        );

        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepository.Object);
        _unitOfWork.Setup(u => u.Categories).Returns(_categoryRepository.Object);
        _unitOfWork.Setup(u => u.CategorizationRules).Returns(_ruleRepository.Object);
        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepository.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _accountRepository
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryExists ? category : null);

        _ruleRepository
            .Setup(r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorizationRule rule, CancellationToken _) => rule);

        _transactionRepository
            .Setup(r => r.GetByAccountIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([matchingTransaction, nonMatchingTransaction]);

        _transactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        _ruleEvaluator
            .SetupSequence(e =>
                e.EvaluateAsync(
                    It.IsAny<Transaction>(),
                    It.IsAny<IReadOnlyList<CategorizationRule>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new RuleEvaluationResult(true, null, new RuleAction(CategoryId: categoryId)))
            .ReturnsAsync(new RuleEvaluationResult(false));

        return new AcceptSuggestedCategorizationRuleCommandHandler(
            _unitOfWork.Object,
            _ruleEvaluator.Object
        );
    }
}
