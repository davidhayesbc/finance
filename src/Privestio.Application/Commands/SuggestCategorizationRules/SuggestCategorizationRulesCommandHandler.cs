using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;

namespace Privestio.Application.Commands.SuggestCategorizationRules;

public class SuggestCategorizationRulesCommandHandler
    : IRequestHandler<SuggestCategorizationRulesCommand, IReadOnlyList<RuleSuggestionResponse>>
{
    private const int MaxRowsForPrompt = 300;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<ITransactionImporter> _importers;
    private readonly IOllamaRuleSuggestionService _ollamaService;
    private readonly IPluginRegistryService _pluginRegistryService;

    public SuggestCategorizationRulesCommandHandler(
        IUnitOfWork unitOfWork,
        IEnumerable<ITransactionImporter> importers,
        IOllamaRuleSuggestionService ollamaService,
        IPluginRegistryService pluginRegistryService
    )
    {
        _unitOfWork = unitOfWork;
        _importers = importers;
        _ollamaService = ollamaService;
        _pluginRegistryService = pluginRegistryService;
    }

    public async Task<IReadOnlyList<RuleSuggestionResponse>> Handle(
        SuggestCategorizationRulesCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot generate suggestions for an account owned by another user."
            );

        var importer =
            _importers.FirstOrDefault(i => i.CanHandle(request.FileName))
            ?? throw new NotSupportedException(
                $"No importer available for file: {request.FileName}"
            );

        ImportMapping? mapping = null;
        if (request.MappingId.HasValue)
        {
            mapping = await _unitOfWork.ImportMappings.GetByIdAsync(
                request.MappingId.Value,
                cancellationToken
            );

            if (
                mapping is not null
                && !_pluginRegistryService.IsRegisteredTransactionImportFormat(mapping.FileFormat)
            )
            {
                throw new InvalidOperationException(
                    $"The selected mapping uses unregistered import source format '{mapping.FileFormat}'."
                );
            }
        }

        var parseResult = await importer.ParseAsync(
            request.FileStream,
            ToPluginImportMapping(mapping),
            cancellationToken
        );

        var rows = parseResult
            .Rows.Where(r => !string.IsNullOrWhiteSpace(r.Description))
            .Take(MaxRowsForPrompt)
            .Select(r => new RuleSuggestionInputRow(r.Description, r.Amount))
            .ToList();

        if (rows.Count == 0)
            return [];

        var drafts = await _ollamaService.SuggestRulesAsync(
            rows,
            request.MaxSuggestions,
            cancellationToken
        );

        var uniqueDrafts = drafts
            .Where(d =>
                !string.IsNullOrWhiteSpace(d.Name)
                && !string.IsNullOrWhiteSpace(d.DescriptionContains)
                && !string.IsNullOrWhiteSpace(d.SuggestedCategoryName)
            )
            .DistinctBy(d => d.DescriptionContains.Trim().ToUpperInvariant())
            .Take(request.MaxSuggestions)
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
            {
                continue;
            }

            if (conditions.MinAmount.HasValue && row.Amount < conditions.MinAmount.Value)
                continue;

            if (conditions.MaxAmount.HasValue && row.Amount > conditions.MaxAmount.Value)
                continue;

            count++;
        }

        return count;
    }

    private static TransactionImportMapping? ToPluginImportMapping(ImportMapping? mapping)
    {
        if (mapping is null)
            return null;

        return new TransactionImportMapping(
            mapping.ColumnMappings,
            mapping.HasHeaderRow,
            mapping.DateFormat,
            mapping.AmountDebitColumn,
            mapping.AmountCreditColumn,
            mapping.AmountSignFlipped,
            mapping.DefaultDate,
            mapping.IgnoreRowPatterns
        );
    }

    private sealed record RuleConditions(
        string DescriptionContains,
        decimal? MinAmount,
        decimal? MaxAmount
    );
}
