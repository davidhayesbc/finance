using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportMappings;

public class GetImportMappingsQueryHandler
    : IRequestHandler<GetImportMappingsQuery, IReadOnlyList<ImportMappingResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetImportMappingsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<ImportMappingResponse>> Handle(
        GetImportMappingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var mappings = await _unitOfWork.ImportMappings.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return mappings
            .Select(m => new ImportMappingResponse
            {
                Id = m.Id,
                Name = m.Name,
                FileFormat = m.FileFormat,
                Institution = m.Institution,
                ColumnMappings = m.ColumnMappings,
                DateFormat = m.DateFormat,
                HasHeaderRow = m.HasHeaderRow,
                AmountDebitColumn = m.AmountDebitColumn,
                AmountCreditColumn = m.AmountCreditColumn,
            })
            .ToList();
    }
}
