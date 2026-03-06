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
    string? AmountCreditColumn = null
) : IRequest<ImportMappingResponse?>;
