using FluentAssertions;
using Moq;
using Privestio.Application.Commands.SuggestCategorizationRulesFromDb;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class SuggestCategorizationRulesFromDbCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IOllamaRuleSuggestionService> _ollamaService = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public async Task Handle_GroupsSimilarRowsBeforeCallingOllama_AndCapsMaxSuggestions()
    {
        // Arrange
        SetupBaseDependencies();

        var rowsSentToOllama = Array.Empty<RuleSuggestionInputRow>();
        var maxSuggestionsSentToOllama = 0;

        _ollamaService
            .Setup(s =>
                s.SuggestRulesAsync(
                    It.IsAny<IReadOnlyList<RuleSuggestionInputRow>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IReadOnlyList<RuleSuggestionInputRow>, int, CancellationToken>(
                (rows, maxSuggestions, _) =>
                {
                    rowsSentToOllama = rows.ToArray();
                    maxSuggestionsSentToOllama = maxSuggestions;
                }
            )
            .ReturnsAsync([]);

        var handler = new SuggestCategorizationRulesFromDbCommandHandler(
            _unitOfWork.Object,
            _ollamaService.Object
        );

        var command = new SuggestCategorizationRulesFromDbCommand(_accountId, _userId, MaxSuggestions: 8);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        maxSuggestionsSentToOllama.Should().Be(4);
        rowsSentToOllama.Should().HaveCount(2);
        rowsSentToOllama.Should().Contain(r => r.Description.Contains("FortisBC", StringComparison.OrdinalIgnoreCase));
        rowsSentToOllama.Should().Contain(r => r.Description.Contains("PAYROLL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_UsesFullRowSetForMatchCount_WhenRowsAreGroupedForPrompt()
    {
        // Arrange
        SetupBaseDependencies();

        _ollamaService
            .Setup(s =>
                s.SuggestRulesAsync(
                    It.IsAny<IReadOnlyList<RuleSuggestionInputRow>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                [
                    new RuleSuggestionDraft(
                        Name: "Fortis recurring utility payment",
                        DescriptionContains: "FORTISBC ENERGY",
                        MinAmount: 60m,
                        MaxAmount: 70m,
                        SuggestedCategoryName: "Utilities",
                        Rationale: "Recurring utility payment"
                    ),
                ]
            );

        var handler = new SuggestCategorizationRulesFromDbCommandHandler(
            _unitOfWork.Object,
            _ollamaService.Object
        );

        var command = new SuggestCategorizationRulesFromDbCommand(_accountId, _userId, MaxSuggestions: 8);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchCount.Should().Be(3);
        result[0].MatchRate.Should().Be(0.75m);
        result[0].MatchSamples.Should().HaveCount(1);
        result[0].MatchSamples[0].Description.Should().ContainEquivalentOf("FortisBC");
        result[0].MatchSamples[0].Amount.Should().Be(63.55m);
        result[0].MatchSamples[0].Frequency.Should().Be(3);
    }

    private void SetupBaseDependencies()
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

        var transactions = new List<Transaction>
        {
            new(
                _accountId,
                DateTime.UtcNow,
                new Money(63.55m),
                "EFT Withdrawal to FortisBC Energy",
                TransactionType.Debit
            ),
            new(
                _accountId,
                DateTime.UtcNow.AddDays(-1),
                new Money(63.55m),
                "EFT Withdrawal to FortisBC Energy",
                TransactionType.Debit
            ),
            new(
                _accountId,
                DateTime.UtcNow.AddDays(-2),
                new Money(63.55m),
                "  eft   withdrawal   to   fortisbc energy  ",
                TransactionType.Debit
            ),
            new(
                _accountId,
                DateTime.UtcNow.AddDays(-3),
                new Money(2500m),
                "PAYROLL DEPOSIT",
                TransactionType.Credit
            ),
        };

        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepository.Object);
        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepository.Object);

        _accountRepository
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepository
            .Setup(r =>
                r.GetUncategorizedByAccountIdAsync(
                    _accountId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(transactions);
    }
}
