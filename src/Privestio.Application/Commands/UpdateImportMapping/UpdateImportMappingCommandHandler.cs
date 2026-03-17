using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateImportMapping;

public class UpdateImportMappingCommandHandler
    : IRequestHandler<UpdateImportMappingCommand, ImportMappingResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateImportMappingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ImportMappingResponse?> Handle(
        UpdateImportMappingCommand request,
        CancellationToken cancellationToken
    )
    {
        var mapping = await _unitOfWork.ImportMappings.GetByIdAsync(request.Id, cancellationToken);
        if (mapping is null || mapping.UserId != request.UserId)
            return null;

        mapping.Rename(request.Name);
        mapping.UpdateMappings(request.ColumnMappings);
        mapping.DateFormat = request.DateFormat;
        mapping.HasHeaderRow = request.HasHeaderRow;
        mapping.AmountDebitColumn = request.AmountDebitColumn;
        mapping.AmountCreditColumn = request.AmountCreditColumn;
        mapping.AmountSignFlipped = request.AmountSignFlipped;

        if (request.BuyKeywords is not null)
            mapping.BuyKeywords = request.BuyKeywords;
        if (request.SellKeywords is not null)
            mapping.SellKeywords = request.SellKeywords;
        if (request.IncomeKeywords is not null)
            mapping.IncomeKeywords = request.IncomeKeywords;
        if (request.CashEquivalentSymbols is not null)
            mapping.CashEquivalentSymbols = request.CashEquivalentSymbols;
        if (request.IgnoreRowPatterns is not null)
            mapping.IgnoreRowPatterns = request.IgnoreRowPatterns;

        await _unitOfWork.ImportMappings.UpdateAsync(mapping, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportMappingResponse
        {
            Id = mapping.Id,
            Name = mapping.Name,
            FileFormat = mapping.FileFormat,
            Institution = mapping.Institution,
            ColumnMappings = mapping.ColumnMappings,
            DateFormat = mapping.DateFormat,
            HasHeaderRow = mapping.HasHeaderRow,
            AmountDebitColumn = mapping.AmountDebitColumn,
            AmountCreditColumn = mapping.AmountCreditColumn,
            BuyKeywords = mapping.BuyKeywords,
            SellKeywords = mapping.SellKeywords,
            IncomeKeywords = mapping.IncomeKeywords,
            CashEquivalentSymbols = mapping.CashEquivalentSymbols,
            IgnoreRowPatterns = mapping.IgnoreRowPatterns,
            AmountSignFlipped = mapping.AmountSignFlipped,
        };
    }
}
