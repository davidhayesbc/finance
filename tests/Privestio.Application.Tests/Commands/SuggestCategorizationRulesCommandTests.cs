using FluentAssertions;
using Moq;
using Privestio.Application.Commands.SuggestCategorizationRules;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class SuggestCategorizationRulesCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IImportMappingRepository> _importMappingRepository = new();
    private readonly Mock<IPluginRegistryService> _pluginRegistryService = new();
    private readonly Mock<IOllamaRuleSuggestionService> _ollamaService = new();
    private readonly Mock<ITransactionImporter> _csvImporter = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WithValidInputs_ReturnsRuleSuggestionsWithSerializedConditions()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SuggestCategorizationRulesCommand(
            CreateDummyStream(),
            "sample.csv",
            _accountId,
            _userId,
            MappingId: null,
            MaxSuggestions: 5
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Fortis recurring utility payment");
        result[0].SuggestedCategoryName.Should().Be("Utilities");
        result[0].Conditions.Should().Contain("DescriptionContains");
        result[0].MatchCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToDifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var account = new Account(
            "Joint",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            Guid.NewGuid()
        );

        _accountRepository
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepository.Object);

        var handler = new SuggestCategorizationRulesCommandHandler(
            _unitOfWork.Object,
            [_csvImporter.Object],
            _ollamaService.Object,
            _pluginRegistryService.Object
        );

        var command = new SuggestCategorizationRulesCommand(
            CreateDummyStream(),
            "sample.csv",
            _accountId,
            _userId,
            MappingId: null,
            MaxSuggestions: 5
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private SuggestCategorizationRulesCommandHandler CreateHandler()
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

        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepository.Object);
        _unitOfWork.Setup(u => u.ImportMappings).Returns(_importMappingRepository.Object);

        _accountRepository
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _pluginRegistryService
            .Setup(p => p.IsRegisteredTransactionImportFormat(It.IsAny<string>()))
            .Returns(true);

        _csvImporter.Setup(i => i.CanHandle("sample.csv")).Returns(true);
        _csvImporter.Setup(i => i.FileFormat).Returns("CSV");
        _csvImporter
            .Setup(i =>
                i.ParseAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<TransactionImportMapping?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ImportParseResult(
                    [
                        new ImportedTransactionRow(
                            DateTime.UtcNow,
                            -63.55m,
                            "EFT Withdrawal to FortisBC Energy"
                        ),
                        new ImportedTransactionRow(
                            DateTime.UtcNow.AddDays(-1),
                            -40.46m,
                            "EFT Withdrawal to FortisBC Energy"
                        ),
                    ],
                    []
                )
            );

        _ollamaService
            .Setup(s =>
                s.SuggestRulesAsync(
                    It.IsAny<IReadOnlyList<RuleSuggestionInputRow>>(),
                    5,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                [
                    new RuleSuggestionDraft(
                        Name: "Fortis recurring utility payment",
                        DescriptionContains: "FortisBC Energ",
                        MinAmount: -120m,
                        MaxAmount: -20m,
                        SuggestedCategoryName: "Utilities",
                        Rationale:
                            "Recurring FortisBC utility debits are stable and match a clear merchant token."
                    ),
                ]
            );

        return new SuggestCategorizationRulesCommandHandler(
            _unitOfWork.Object,
            [_csvImporter.Object],
            _ollamaService.Object,
            _pluginRegistryService.Object
        );
    }

    private static Stream CreateDummyStream() => new MemoryStream("x"u8.ToArray());
}
