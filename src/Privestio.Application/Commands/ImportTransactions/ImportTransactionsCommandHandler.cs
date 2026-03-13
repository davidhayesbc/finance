using System.Text.Json;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.ImportTransactions;

public class ImportTransactionsCommandHandler
    : IRequestHandler<ImportTransactionsCommand, ImportResultResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<ITransactionImporter> _importers;
    private readonly TransactionFingerprintService _fingerprintService;

    public ImportTransactionsCommandHandler(
        IUnitOfWork unitOfWork,
        IEnumerable<ITransactionImporter> importers,
        TransactionFingerprintService fingerprintService
    )
    {
        _unitOfWork = unitOfWork;
        _importers = importers;
        _fingerprintService = fingerprintService;
    }

    public async Task<ImportResultResponse> Handle(
        ImportTransactionsCommand request,
        CancellationToken cancellationToken
    )
    {
        var importer =
            _importers.FirstOrDefault(i => i.CanHandle(request.FileName))
            ?? throw new NotSupportedException(
                $"No importer available for file: {request.FileName}"
            );

        // Load mapping if specified
        ImportMapping? mapping = null;
        if (request.MappingId.HasValue)
        {
            mapping = await _unitOfWork.ImportMappings.GetByIdAsync(
                request.MappingId.Value,
                cancellationToken
            );
        }

        // Create the import batch
        var batch = new ImportBatch(
            request.FileName,
            importer.FileFormat,
            request.UserId,
            request.MappingId
        );
        batch.Status = ImportStatus.Processing;

        // For preview-only, we don't persist the batch
        if (request.Policy != ImportPolicy.PreviewOnly)
        {
            await _unitOfWork.ImportBatches.AddAsync(batch, cancellationToken);
        }

        // Parse the file
        var parseResult = await importer.ParseAsync(request.FileStream, mapping, cancellationToken);

        // FailFast: abort if any parse errors
        if (request.Policy == ImportPolicy.FailFast && parseResult.Errors.Count > 0)
        {
            batch.Fail(
                string.Join(
                    "; ",
                    parseResult.Errors.Select(e => $"Row {e.RowNumber}: {e.ErrorMessage}")
                )
            );

            if (request.Policy != ImportPolicy.PreviewOnly)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new ImportResultResponse
            {
                ImportBatchId = batch.Id,
                TotalRows = parseResult.Rows.Count + parseResult.Errors.Count,
                ImportedCount = 0,
                DuplicateCount = 0,
                ErrorCount = parseResult.Errors.Count,
                Status = batch.Status.ToString(),
                Errors = parseResult
                    .Errors.Select(e => new ImportErrorDetail
                    {
                        RowNumber = e.RowNumber,
                        Message = e.ErrorMessage,
                        RawData = e.RawData,
                    })
                    .ToList(),
            };
        }

        // Generate fingerprints for all parsed rows, tracking per-base-fingerprint
        // occurrence counts so that legitimate repeated identical transactions on the
        // same day (e.g. two $5 coffee purchases) get distinct, stable fingerprints.
        var baseOccurrences = new Dictionary<string, int>();
        var rowsWithFingerprints = new List<(ImportedTransactionRow Row, string Fingerprint)>();
        foreach (var row in parseResult.Rows)
        {
            var baseFingerprint = _fingerprintService.GenerateFingerprint(
                request.AccountId,
                row.Date,
                new Money(row.Amount),
                row.Description,
                row.ExternalId
            );
            var occurrence = baseOccurrences.GetValueOrDefault(baseFingerprint, 0);
            baseOccurrences[baseFingerprint] = occurrence + 1;

            var fingerprint =
                occurrence == 0
                    ? baseFingerprint
                    : _fingerprintService.GenerateFingerprint(
                        request.AccountId,
                        row.Date,
                        new Money(row.Amount),
                        row.Description,
                        row.ExternalId,
                        occurrence
                    );
            rowsWithFingerprints.Add((row, fingerprint));
        }

        // Batch check for existing fingerprints (duplicate detection)
        var allFingerprints = rowsWithFingerprints.Select(r => r.Fingerprint);
        var existingFingerprints = await _unitOfWork.Transactions.GetExistingFingerprintsAsync(
            allFingerprints,
            cancellationToken
        );

        // Build transactions, skipping duplicates
        var transactions = new List<Transaction>();
        var duplicateCount = 0;

        foreach (var item in rowsWithFingerprints)
        {
            if (existingFingerprints.Contains(item.Fingerprint))
            {
                duplicateCount++;
                continue;
            }

            var type = item.Row.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;
            var transaction = new Transaction(
                request.AccountId,
                item.Row.Date,
                new Money(Math.Abs(item.Row.Amount)),
                item.Row.Description,
                type
            )
            {
                ImportFingerprint = item.Fingerprint,
                ImportBatchId = batch.Id,
                ExternalId = item.Row.ExternalId,
                Notes = item.Row.Notes,
            };

            transactions.Add(transaction);
        }

        // PreviewOnly: return results without persisting
        if (request.Policy == ImportPolicy.PreviewOnly)
        {
            return new ImportResultResponse
            {
                ImportBatchId = batch.Id,
                TotalRows = parseResult.Rows.Count + parseResult.Errors.Count,
                ImportedCount = transactions.Count,
                DuplicateCount = duplicateCount,
                ErrorCount = parseResult.Errors.Count,
                Status = "Preview",
                Errors = parseResult
                    .Errors.Select(e => new ImportErrorDetail
                    {
                        RowNumber = e.RowNumber,
                        Message = e.ErrorMessage,
                        RawData = e.RawData,
                    })
                    .ToList(),
            };
        }

        // Persist transactions
        if (transactions.Count > 0)
        {
            await _unitOfWork.Transactions.AddRangeAsync(transactions, cancellationToken);
        }

        // Update batch metrics and store error details for diagnostics
        batch.Complete(
            rowCount: parseResult.Rows.Count,
            successCount: transactions.Count,
            errorCount: parseResult.Errors.Count,
            duplicateCount: duplicateCount
        );

        if (parseResult.Errors.Count > 0)
        {
            batch.ErrorDetails = JsonSerializer.Serialize(
                parseResult.Errors.Select(e => new
                {
                    e.RowNumber,
                    Message = e.ErrorMessage,
                    e.RawData,
                })
            );
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportResultResponse
        {
            ImportBatchId = batch.Id,
            TotalRows = parseResult.Rows.Count + parseResult.Errors.Count,
            ImportedCount = transactions.Count,
            DuplicateCount = duplicateCount,
            ErrorCount = parseResult.Errors.Count,
            Status = batch.Status.ToString(),
            Errors = parseResult
                .Errors.Select(e => new ImportErrorDetail
                {
                    RowNumber = e.RowNumber,
                    Message = e.ErrorMessage,
                    RawData = e.RawData,
                })
                .ToList(),
        };
    }
}
