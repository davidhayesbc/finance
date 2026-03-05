using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetImportBatch;

public class GetImportBatchQueryHandler : IRequestHandler<GetImportBatchQuery, ImportBatchResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetImportBatchQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportBatchResponse?> Handle(
        GetImportBatchQuery request,
        CancellationToken cancellationToken
    )
    {
        var batch = await _unitOfWork.ImportBatches.GetByIdAsync(
            request.BatchId,
            cancellationToken
        );

        if (batch is null)
            return null;

        var errors = DeserializeErrors(batch.ErrorDetails);
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
            SuccessRate = rowCount > 0 ? Math.Round((decimal)batch.SuccessCount / rowCount, 2) : 0m,
            DuplicateRate =
                rowCount > 0 ? Math.Round((decimal)batch.DuplicateCount / rowCount, 2) : 0m,
            Errors = errors,
        };
    }

    private static IReadOnlyList<ImportErrorDetail> DeserializeErrors(string? errorDetails)
    {
        if (string.IsNullOrWhiteSpace(errorDetails))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ImportErrorDetail>>(
                    errorDetails,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
