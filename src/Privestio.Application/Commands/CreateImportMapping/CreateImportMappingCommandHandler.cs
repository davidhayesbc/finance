using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.CreateImportMapping;

public class CreateImportMappingCommandHandler
    : IRequestHandler<CreateImportMappingCommand, ImportMappingResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateImportMappingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ImportMappingResponse> Handle(
        CreateImportMappingCommand request,
        CancellationToken cancellationToken
    )
    {
        var mapping = new ImportMapping(
            request.Name,
            request.FileFormat,
            request.UserId,
            request.ColumnMappings,
            request.Institution
        )
        {
            DateFormat = request.DateFormat,
            HasHeaderRow = request.HasHeaderRow,
            AmountDebitColumn = request.AmountDebitColumn,
            AmountCreditColumn = request.AmountCreditColumn,
            AmountSignFlipped = request.AmountSignFlipped,
            DefaultDate = request.DefaultDate,
        };

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

        await _unitOfWork.ImportMappings.AddAsync(mapping, cancellationToken);
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
            DefaultDate = mapping.DefaultDate,
        };
    }
}
