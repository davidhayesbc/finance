using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateImportMapping;

public record UpdateImportMappingCommand(
    Guid Id,
    string Name,
    Dictionary<string, string> ColumnMappings,
    Guid UserId,
    string? DateFormat = null,
    bool HasHeaderRow = true,
    string? AmountDebitColumn = null,
    string? AmountCreditColumn = null,
    List<string>? BuyKeywords = null,
    List<string>? SellKeywords = null,
    List<string>? IncomeKeywords = null,
    List<string>? CashEquivalentSymbols = null,
    List<string>? IgnoreRowPatterns = null,
    bool AmountSignFlipped = false
) : IRequest<ImportMappingResponse?>;
