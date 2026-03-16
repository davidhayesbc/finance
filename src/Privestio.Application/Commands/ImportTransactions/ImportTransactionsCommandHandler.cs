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
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot import into an account owned by another user."
            );

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
        var importedRows = new List<ImportedTransactionRow>();
        var duplicateCount = 0;

        foreach (var item in rowsWithFingerprints)
        {
            if (existingFingerprints.Contains(item.Fingerprint))
            {
                duplicateCount++;
                continue;
            }

            var type = item.Row.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;
            var normalizedDescription =
                Truncate(item.Row.Description, 500) ?? "Imported Transaction";
            var transaction = new Transaction(
                request.AccountId,
                item.Row.Date,
                new Money(Math.Abs(item.Row.Amount)),
                normalizedDescription,
                type
            )
            {
                ImportFingerprint = item.Fingerprint,
                ImportBatchId = batch.Id,
                ExternalId = Truncate(item.Row.ExternalId, 200),
                Notes = Truncate(item.Row.Notes, 2000),
                SettlementDate = item.Row.SettlementDate,
                ActivityType = Truncate(item.Row.ActivityType, 64),
                ActivitySubType = Truncate(item.Row.ActivitySubType, 128),
                Direction = Truncate(item.Row.Direction, 32),
                Symbol = NormalizeSymbol(item.Row.Symbol, 32),
                SecurityName = Truncate(item.Row.SecurityName, 256),
                Quantity = NormalizeNullableDecimal(item.Row.Quantity),
                UnitPrice = NormalizeNullableDecimal(item.Row.UnitPrice),
            };

            transactions.Add(transaction);
            importedRows.Add(item.Row);
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

            // Build/refresh holdings and lots for investment accounts from imported trade rows.
            if (account.AccountType == AccountType.Investment)
            {
                await UpsertInvestmentPositionsFromImportAsync(
                    request.AccountId,
                    account.Currency,
                    importedRows,
                    cancellationToken
                );
            }
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
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
        {
            var innerMessage = ex.InnerException?.Message ?? "No inner exception details.";
            throw new InvalidOperationException($"Import persistence failed: {innerMessage}", ex);
        }

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

    private async Task UpsertInvestmentPositionsFromImportAsync(
        Guid accountId,
        string accountCurrency,
        IEnumerable<ImportedTransactionRow> rows,
        CancellationToken cancellationToken
    )
    {
        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(accountId, cancellationToken);
        var bySymbol = holdings.ToDictionary(h => h.Symbol, StringComparer.OrdinalIgnoreCase);
        var newHoldingIds = new HashSet<Guid>();
        var pendingLots = new List<Lot>();
        var pendingIncomeCredits = new List<(DateOnly Date, decimal Amount)>();

        foreach (var row in rows)
        {
            if (IsIncomeCashCredit(row))
            {
                pendingIncomeCredits.Add((DateOnly.FromDateTime(row.Date), row.Amount));
            }

            if (string.IsNullOrWhiteSpace(row.Symbol) || row.Quantity is null || row.Quantity <= 0)
                continue;

            var symbol = NormalizeSymbol(row.Symbol, 20);
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            var quantityMagnitude = Math.Abs(row.Quantity.Value);
            var signedQuantityDelta = DetermineSignedQuantityDelta(row, quantityMagnitude);
            if (signedQuantityDelta == 0)
                continue;

            var unitCost = row.UnitPrice ?? InferUnitPriceFromAmount(row, quantityMagnitude);
            if (unitCost <= 0)
                continue;

            var currency = accountCurrency;

            if (!bySymbol.TryGetValue(symbol, out var holding))
            {
                if (signedQuantityDelta <= 0)
                    continue;

                holding = new Holding(
                    accountId,
                    symbol,
                    Truncate(
                        string.IsNullOrWhiteSpace(row.SecurityName) ? symbol : row.SecurityName,
                        200
                    ) ?? symbol,
                    signedQuantityDelta,
                    new Money(unitCost, currency)
                );
                await _unitOfWork.Holdings.AddAsync(holding, cancellationToken);
                bySymbol[symbol] = holding;
                newHoldingIds.Add(holding.Id);
            }
            else
            {
                var previousQuantity = holding.Quantity;
                var nextQuantity = Math.Max(0m, previousQuantity + signedQuantityDelta);

                var nextAverageCost = holding.AverageCostPerUnit.Amount;
                if (signedQuantityDelta > 0)
                {
                    var existingCostBasis = previousQuantity * holding.AverageCostPerUnit.Amount;
                    var newCostBasis = signedQuantityDelta * unitCost;
                    nextAverageCost =
                        nextQuantity > 0
                            ? (existingCostBasis + newCostBasis) / nextQuantity
                            : holding.AverageCostPerUnit.Amount;
                }

                holding.Update(
                    nextQuantity,
                    new Money(Math.Round(nextAverageCost, 8, MidpointRounding.ToEven), currency),
                    holding.Notes
                );

                // If this holding was created earlier in this same import pass,
                // keep it in Added state so EF inserts it once with final values.
                if (!newHoldingIds.Contains(holding.Id))
                {
                    await _unitOfWork.Holdings.UpdateAsync(holding, cancellationToken);
                }
            }

            if (signedQuantityDelta > 0)
            {
                var lotSource = ResolveLotSource(row, symbol, pendingIncomeCredits);
                var lot = new Lot(
                    holding.Id,
                    row.SettlementDate ?? DateOnly.FromDateTime(row.Date),
                    signedQuantityDelta,
                    new Money(Math.Round(unitCost, 8, MidpointRounding.ToEven), currency),
                    source: lotSource,
                    notes: Truncate(row.Notes, 2000)
                );

                pendingLots.Add(lot);
            }
        }

        if (pendingLots.Count == 0)
            return;

        // Flush transactions/holdings first so Lots always reference persisted Holding rows.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var lot in pendingLots)
        {
            await _unitOfWork.Lots.AddAsync(lot, cancellationToken);
        }
    }

    private static string? ResolveLotSource(
        ImportedTransactionRow row,
        string symbol,
        List<(DateOnly Date, decimal Amount)> pendingIncomeCredits
    )
    {
        if (IsCashEquivalentSymbol(symbol) && TryMatchIncomeReinvestment(row, pendingIncomeCredits))
            return "ReinvestedIncome";

        return Truncate(
            string.IsNullOrWhiteSpace(row.ActivityType) ? "Import" : row.ActivityType,
            100
        );
    }

    private static bool TryMatchIncomeReinvestment(
        ImportedTransactionRow row,
        List<(DateOnly Date, decimal Amount)> pendingIncomeCredits
    )
    {
        if (row.Amount >= 0)
            return false;

        var activityType = row.ActivityType?.Trim();
        var activitySubType = row.ActivitySubType?.Trim();
        var direction = row.Direction?.Trim();

        var isTradeBuy =
            MatchesAny(activityType, ["trade"]) && MatchesAny(activitySubType, ["buy"])
            || MatchesAny(direction, ["buy", "long"]);

        if (!isTradeBuy)
            return false;

        var tradeDate = row.SettlementDate ?? DateOnly.FromDateTime(row.Date);
        var tradeAmount = Math.Abs(row.Amount);
        const decimal amountTolerance = 0.02m;
        const int maxDayGap = 3;

        for (var i = 0; i < pendingIncomeCredits.Count; i++)
        {
            var candidate = pendingIncomeCredits[i];
            var dayDiff = Math.Abs(tradeDate.DayNumber - candidate.Date.DayNumber);
            var amountDiff = Math.Abs(candidate.Amount - tradeAmount);

            if (dayDiff <= maxDayGap && amountDiff <= amountTolerance)
            {
                pendingIncomeCredits.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private static bool IsIncomeCashCredit(ImportedTransactionRow row) =>
        row.Amount > 0
        && (
            MatchesAny(row.ActivityType, ["interest", "dividend", "distribution"])
            || MatchesAny(row.ActivitySubType, ["interest", "dividend", "distribution"])
        );

    private static bool IsCashEquivalentSymbol(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        return normalized.StartsWith("CASH", StringComparison.Ordinal);
    }

    private static decimal DetermineSignedQuantityDelta(
        ImportedTransactionRow row,
        decimal quantityMagnitude
    )
    {
        var direction = row.Direction?.Trim().ToLowerInvariant();
        var activity = row.ActivityType?.Trim().ToLowerInvariant();
        var subType = row.ActivitySubType?.Trim().ToLowerInvariant();

        var isSell =
            MatchesAny(direction, ["sell", "redeem", "withdraw", "out"])
            || MatchesAny(activity, ["sell", "redeem", "withdraw"])
            || MatchesAny(subType, ["sell", "redeem", "withdraw"]);

        var isBuy =
            MatchesAny(direction, ["buy", "deposit", "in", "long"])
            || MatchesAny(activity, ["buy", "purchase", "contribution", "reinvest"])
            || MatchesAny(subType, ["buy", "purchase", "contribution", "reinvest"]);

        if (isSell)
            return -quantityMagnitude;
        if (isBuy)
            return quantityMagnitude;

        // Fallback heuristic: negative cash amount implies buy; positive implies sell.
        if (row.Amount < 0)
            return quantityMagnitude;
        if (row.Amount > 0)
            return -quantityMagnitude;

        return 0m;
    }

    private static bool MatchesAny(string? value, IReadOnlyList<string> terms) =>
        !string.IsNullOrWhiteSpace(value)
        && terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static decimal InferUnitPriceFromAmount(ImportedTransactionRow row, decimal quantity) =>
        quantity <= 0
            ? 0m
            : Math.Round(Math.Abs(row.Amount) / quantity, 8, MidpointRounding.ToEven);

    private static decimal? NormalizeNullableDecimal(decimal? value) =>
        value.HasValue ? Math.Round(value.Value, 8, MidpointRounding.ToEven) : null;

    private static string? NormalizeSymbol(string? value, int maxLength)
    {
        var trimmed = Truncate(value?.Trim().ToUpperInvariant(), maxLength);
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
