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
        };

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
        };
    }
}
