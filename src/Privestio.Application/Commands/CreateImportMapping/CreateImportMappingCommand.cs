using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateImportMapping;

public record CreateImportMappingCommand(
    string Name,
    string FileFormat,
    Guid UserId,
    Dictionary<string, string> ColumnMappings,
    string? Institution = null,
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
) : IRequest<ImportMappingResponse>;
