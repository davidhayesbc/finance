using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportBatches;

public class GetImportBatchesQueryHandler
    : IRequestHandler<GetImportBatchesQuery, IReadOnlyList<ImportBatchResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetImportBatchesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ImportBatchResponse>> Handle(
        GetImportBatchesQuery request,
        CancellationToken cancellationToken
    )
    {
        var batches = await _unitOfWork.ImportBatches.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return batches
            .Select(batch =>
            {
                var rowCount = batch.RowCount;
                return new ImportBatchResponse
                {
                    Id = batch.Id,
                    FileName = batch.FileName,
                    FileFormat = batch.FileFormat,
                    ImportDate = batch.ImportDate,
                    RowCount = rowCount,
                    SuccessCount = batch.SuccessCount,
                    ErrorCount = batch.ErrorCount,
                    DuplicateCount = batch.DuplicateCount,
                    Status = batch.Status.ToString(),
                    SuccessRate =
                        rowCount > 0 ? Math.Round((decimal)batch.SuccessCount / rowCount, 2) : 0m,
                    DuplicateRate =
                        rowCount > 0 ? Math.Round((decimal)batch.DuplicateCount / rowCount, 2) : 0m,
                };
            })
            .ToList();
    }
}
