using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
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
    private readonly SecurityResolutionService _securityResolutionService;
    private readonly IPriceFeedProvider _priceFeedProvider;
    private readonly IPluginRegistryService _pluginRegistry;
    private readonly PricingOptions _pricingOptions;
    private readonly ILogger<ImportTransactionsCommandHandler> _logger;

    // Default keyword lists — match the original hardcoded values so existing imports
    // continue to work when no mapping-level overrides are configured.
    private static readonly ImportClassificationConfig DefaultConfig = new(
        BuyKeywords: ["buy", "purchase", "contribution", "reinvest", "deposit", "in", "long"],
        SellKeywords: ["sell", "redeem", "withdraw", "out"],
        IncomeKeywords: ["interest", "dividend", "distribution"],
        CashEquivalentSymbols: ["CASH"]
    );

    public ImportTransactionsCommandHandler(
        IUnitOfWork unitOfWork,
        IEnumerable<ITransactionImporter> importers,
        TransactionFingerprintService fingerprintService,
        SecurityResolutionService securityResolutionService,
        IPriceFeedProvider priceFeedProvider,
        IPluginRegistryService pluginRegistry,
        IOptions<PricingOptions> pricingOptions,
        ILogger<ImportTransactionsCommandHandler> logger
    )
    {
        _unitOfWork = unitOfWork;
        _importers = importers;
        _fingerprintService = fingerprintService;
        _securityResolutionService = securityResolutionService;
        _priceFeedProvider = priceFeedProvider;
        _pluginRegistry = pluginRegistry;
        _pricingOptions = pricingOptions.Value;
        _logger = logger;
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

            if (
                mapping is not null
                && !_pluginRegistry.IsRegisteredTransactionImportFormat(mapping.FileFormat)
            )
            {
                throw new InvalidOperationException(
                    $"The selected mapping uses unregistered import source format '{mapping.FileFormat}'."
                );
            }
        }

        // Build classification config from the mapping, falling back to defaults.
        // Empty lists (e.g. rows migrated before this feature was added) also fall back
        // to defaults so no existing import is silently broken.
        var config = mapping is not null
            ? new ImportClassificationConfig(
                BuyKeywords: mapping.BuyKeywords.Count > 0
                    ? mapping.BuyKeywords
                    : DefaultConfig.BuyKeywords,
                SellKeywords: mapping.SellKeywords.Count > 0
                    ? mapping.SellKeywords
                    : DefaultConfig.SellKeywords,
                IncomeKeywords: mapping.IncomeKeywords.Count > 0
                    ? mapping.IncomeKeywords
                    : DefaultConfig.IncomeKeywords,
                CashEquivalentSymbols: mapping.CashEquivalentSymbols.Count > 0
                    ? mapping.CashEquivalentSymbols
                    : DefaultConfig.CashEquivalentSymbols
            )
            : DefaultConfig;

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
        var parseResult = await importer.ParseAsync(
            request.FileStream,
            ToPluginImportMapping(mapping),
            cancellationToken
        );

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
                row.ExternalId,
                institution: account.Institution
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
                        occurrence,
                        institution: account.Institution
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
        var ownerAccounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            account.OwnerId,
            cancellationToken
        );

        foreach (var item in rowsWithFingerprints)
        {
            if (existingFingerprints.Contains(item.Fingerprint))
            {
                duplicateCount++;
                continue;
            }

            var inferredPresentation = InferImportedTransactionPresentation(
                item.Row,
                account,
                ownerAccounts
            );
            var normalizedDescription =
                Truncate(inferredPresentation.Description, 500) ?? "Imported Transaction";
            // Transfer transactions carry sign in the Amount (negative = outgoing, positive = incoming),
            // matching how CreateTransferCommandHandler stores transfers.
            // Credit/Debit transactions always store a positive Amount; sign is encoded in the Type.
            var transactionAmount =
                inferredPresentation.Type == TransactionType.Transfer
                    ? new Money(item.Row.Amount)
                    : new Money(Math.Abs(item.Row.Amount));
            var transaction = new Transaction(
                request.AccountId,
                item.Row.Date,
                transactionAmount,
                normalizedDescription,
                inferredPresentation.Type
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

            if (
                inferredPresentation.Type == TransactionType.Transfer
                && inferredPresentation.CounterpartyAccountId.HasValue
            )
            {
                await TryLinkToExistingTransferAsync(
                    transaction,
                    inferredPresentation,
                    cancellationToken
                );
            }

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
                    config,
                    mapping?.Institution,
                    cancellationToken
                );
            }
        }

        // Determine earliest investment row date for historical price fetch scope.
        var earliestInvestmentDate = importedRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Symbol))
            .Select(r => (DateOnly?)DateOnly.FromDateTime(r.Date))
            .Min();

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

        // After persisting, fetch historical prices for investment accounts so portfolio
        // valuation has price data from the first transaction date forward.
        if (account.AccountType == AccountType.Investment && earliestInvestmentDate.HasValue)
        {
            await FetchHistoricalPricesForImportedSecuritiesAsync(
                request.AccountId,
                earliestInvestmentDate.Value,
                cancellationToken
            );
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

    private async Task FetchHistoricalPricesForImportedSecuritiesAsync(
        Guid accountId,
        DateOnly fromDate,
        CancellationToken cancellationToken
    )
    {
        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(accountId, cancellationToken);
        var securities = holdings
            .Where(h => h.Quantity > 0m && h.Security is not null && !h.Security.IsCashEquivalent)
            .Select(h => h.Security!)
            .DistinctBy(s => s.Id)
            .ToList();

        if (securities.Count == 0)
            return;

        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var allQuotes = new List<PriceQuote>();

        foreach (var security in securities)
        {
            var order = security.PricingProviderOrder ?? _pricingOptions.ProviderOrder;
            var lookup = _securityResolutionService.BuildPriceLookup(security, order);
            try
            {
                var quotes = await _priceFeedProvider.GetHistoricalPricesAsync(
                    lookup,
                    fromDate,
                    toDate,
                    cancellationToken
                );
                allQuotes.AddRange(quotes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to fetch historical prices for {Symbol} during import",
                    security.DisplaySymbol
                );
            }
        }

        var distinctQuotes = allQuotes
            .Where(q => q.Price > 0m)
            .GroupBy(q => (q.SecurityId, q.AsOfDate))
            .Select(g => g.First())
            .ToList();

        if (distinctQuotes.Count == 0)
            return;

        var existingKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
            distinctQuotes.Select(q => (q.SecurityId, q.AsOfDate)),
            cancellationToken
        );

        var securityById = securities.ToDictionary(s => s.Id);
        var newEntries = distinctQuotes
            .Where(q => !existingKeys.Contains((q.SecurityId, q.AsOfDate)))
            .Where(q => securityById.ContainsKey(q.SecurityId))
            .Select(q =>
            {
                var security = securityById[q.SecurityId];
                return new PriceHistory(
                    q.SecurityId,
                    security.DisplaySymbol,
                    q.Symbol,
                    new Money(q.Price, q.Currency),
                    q.AsOfDate,
                    q.Source ?? _priceFeedProvider.ProviderName
                );
            })
            .ToList();

        if (newEntries.Count > 0)
        {
            await _unitOfWork.PriceHistories.AddRangeAsync(newEntries, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static TransactionImportMapping? ToPluginImportMapping(ImportMapping? mapping)
    {
        if (mapping is null)
            return null;

        return new TransactionImportMapping(
            ColumnMappings: new Dictionary<string, string>(mapping.ColumnMappings),
            HasHeaderRow: mapping.HasHeaderRow,
            DateFormat: mapping.DateFormat,
            AmountDebitColumn: mapping.AmountDebitColumn,
            AmountCreditColumn: mapping.AmountCreditColumn,
            AmountSignFlipped: mapping.AmountSignFlipped,
            DefaultDate: mapping.DefaultDate,
            IgnoreRowPatterns: mapping.IgnoreRowPatterns
        );
    }

    private async Task UpsertInvestmentPositionsFromImportAsync(
        Guid accountId,
        string accountCurrency,
        IEnumerable<ImportedTransactionRow> rows,
        ImportClassificationConfig config,
        string? institution,
        CancellationToken cancellationToken
    )
    {
        var holdings = await _unitOfWork.Holdings.GetByAccountIdAsync(accountId, cancellationToken);
        var bySecurityId = holdings.ToDictionary(h => h.SecurityId);
        var newHoldingIds = new HashSet<Guid>();
        var pendingLots = new List<Lot>();
        var pendingIncomeCredits = new List<(DateOnly Date, decimal Amount)>();

        foreach (var row in rows)
        {
            if (IsIncomeCashCredit(row, config))
            {
                pendingIncomeCredits.Add((DateOnly.FromDateTime(row.Date), row.Amount));
            }

            if (string.IsNullOrWhiteSpace(row.Symbol) || row.Quantity is null || row.Quantity == 0)
                continue;

            var symbol = NormalizeSymbol(row.Symbol, 20);
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            var quantityMagnitude = Math.Abs(row.Quantity.Value);
            var signedQuantityDelta = DetermineSignedQuantityDelta(row, quantityMagnitude, config);
            if (signedQuantityDelta == 0)
                continue;

            var unitCost = row.UnitPrice ?? InferUnitPriceFromAmount(row, quantityMagnitude);
            if (unitCost <= 0)
                continue;

            var identifiers = BuildSecurityIdentifiers(row.Cusip, row.Isin);
            var currency = row.Currency ?? accountCurrency;
            var security = await _securityResolutionService.ResolveOrCreateAsync(
                symbol,
                Truncate(
                    string.IsNullOrWhiteSpace(row.SecurityName) ? symbol : row.SecurityName,
                    200
                ) ?? symbol,
                currency,
                isCashEquivalent: IsCashEquivalentSymbol(symbol, config),
                source: institution ?? "ImportTransactions",
                exchange: row.Exchange,
                identifiers: identifiers,
                cancellationToken: cancellationToken
            );

            if (!bySecurityId.TryGetValue(security.Id, out var holding))
            {
                if (signedQuantityDelta <= 0)
                    continue;

                holding = new Holding(
                    accountId,
                    security.Id,
                    security.DisplaySymbol,
                    security.Name,
                    signedQuantityDelta,
                    new Money(unitCost, currency)
                );
                holding.RebindSecurity(security);
                await _unitOfWork.Holdings.AddAsync(holding, cancellationToken);
                bySecurityId[security.Id] = holding;
                newHoldingIds.Add(holding.Id);
            }
            else
            {
                holding.RebindSecurity(security);
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
                var lotSource = ResolveLotSource(row, security, pendingIncomeCredits, config);
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
        Security security,
        List<(DateOnly Date, decimal Amount)> pendingIncomeCredits,
        ImportClassificationConfig config
    )
    {
        if (
            security.IsCashEquivalent
            && TryMatchIncomeReinvestment(row, pendingIncomeCredits, config)
        )
            return "ReinvestedIncome";

        return Truncate(
            string.IsNullOrWhiteSpace(row.ActivityType) ? "Import" : row.ActivityType,
            100
        );
    }

    private static bool TryMatchIncomeReinvestment(
        ImportedTransactionRow row,
        List<(DateOnly Date, decimal Amount)> pendingIncomeCredits,
        ImportClassificationConfig config
    )
    {
        if (row.Amount >= 0)
            return false;

        var activityType = row.ActivityType?.Trim();
        var activitySubType = row.ActivitySubType?.Trim();
        var direction = row.Direction?.Trim();

        var isTradeBuy =
            MatchesAny(activitySubType, config.BuyKeywords)
            || MatchesAny(direction, config.BuyKeywords)
            || MatchesAny(activityType, config.BuyKeywords);

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

    private static bool IsIncomeCashCredit(
        ImportedTransactionRow row,
        ImportClassificationConfig config
    ) =>
        row.Amount > 0
        && (
            MatchesAny(row.ActivityType, config.IncomeKeywords)
            || MatchesAny(row.ActivitySubType, config.IncomeKeywords)
        );

    private static bool IsCashEquivalentSymbol(string symbol, ImportClassificationConfig config)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        return config.CashEquivalentSymbols.Any(prefix =>
            normalized.StartsWith(prefix.Trim().ToUpperInvariant(), StringComparison.Ordinal)
        );
    }

    private static decimal DetermineSignedQuantityDelta(
        ImportedTransactionRow row,
        decimal quantityMagnitude,
        ImportClassificationConfig config
    )
    {
        var direction = row.Direction?.Trim().ToLowerInvariant();
        var activity = row.ActivityType?.Trim().ToLowerInvariant();
        var subType = row.ActivitySubType?.Trim().ToLowerInvariant();

        var isSell =
            MatchesAny(direction, config.SellKeywords)
            || MatchesAny(activity, config.SellKeywords)
            || MatchesAny(subType, config.SellKeywords);

        var isBuy =
            MatchesAny(direction, config.BuyKeywords)
            || MatchesAny(activity, config.BuyKeywords)
            || MatchesAny(subType, config.BuyKeywords);

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

    private static ImportedTransactionPresentation InferImportedTransactionPresentation(
        ImportedTransactionRow row,
        Account destinationAccount,
        IReadOnlyList<Account> ownerAccounts
    )
    {
        var defaultType = row.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;

        if (
            destinationAccount.AccountType != AccountType.Banking
            || !string.Equals(destinationAccount.Institution, "Tangerine", StringComparison.OrdinalIgnoreCase)
        )
        {
            return new ImportedTransactionPresentation(defaultType, row.Description);
        }

        var direction = InferTangerineTransferDirection(row);
        if (direction is null)
            return new ImportedTransactionPresentation(defaultType, row.Description);

        var counterpartAccountNumber = ExtractTangerineCounterpartyAccountNumber(row);
        var counterpartName = ResolveCounterpartyAccountName(
            counterpartAccountNumber,
            destinationAccount,
            ownerAccounts
        );
        var counterpartAccountId = ownerAccounts
            .FirstOrDefault(account =>
                account.Id != destinationAccount.Id
                && !string.IsNullOrWhiteSpace(counterpartAccountNumber)
                && string.Equals(
                    account.AccountNumber,
                    counterpartAccountNumber,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            ?.Id;

        var counterpartLabel = counterpartName
            ?? counterpartAccountNumber
            ?? (direction == TransferDirection.Incoming ? "Tangerine savings" : "Tangerine account");

        var description = direction == TransferDirection.Incoming
            ? $"Transfer from {counterpartLabel}"
            : $"Transfer to {counterpartLabel}";

        return new ImportedTransactionPresentation(
            TransactionType.Transfer,
            description,
            counterpartAccountId,
            direction
        );
    }

    private async Task TryLinkToExistingTransferAsync(
        Transaction transaction,
        ImportedTransactionPresentation inferredPresentation,
        CancellationToken cancellationToken
    )
    {
        var counterpartyAccountId = inferredPresentation.CounterpartyAccountId;
        if (!counterpartyAccountId.HasValue)
            return;

        var candidates = await _unitOfWork.Transactions.GetByAccountIdAsync(
            counterpartyAccountId.Value,
            cancellationToken
        ) ?? [];

        var bestMatch = candidates
            .Where(candidate =>
                candidate.Type == TransactionType.Transfer
                && candidate.LinkedTransferId is null
                && Math.Abs(Math.Abs(candidate.Amount.Amount) - Math.Abs(transaction.Amount.Amount)) <= 0.01m
                && Math.Abs((candidate.Date.Date - transaction.Date.Date).Days) <= 3
                && IsOppositeTransferDirection(candidate.Description, inferredPresentation.Direction)
            )
            .OrderBy(candidate => Math.Abs((candidate.Date.Date - transaction.Date.Date).Days))
            .ThenByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefault();

        if (bestMatch is null)
            return;

        transaction.LinkedTransferId = bestMatch.Id;
        bestMatch.LinkedTransferId = transaction.Id;
        await _unitOfWork.Transactions.UpdateAsync(bestMatch, cancellationToken);
    }

    private static bool IsOppositeTransferDirection(
        string description,
        TransferDirection? incomingOrOutgoing
    )
    {
        if (!incomingOrOutgoing.HasValue)
            return true;

        var isFrom = description.StartsWith("Transfer from", StringComparison.OrdinalIgnoreCase);
        var isTo = description.StartsWith("Transfer to", StringComparison.OrdinalIgnoreCase);

        return incomingOrOutgoing.Value == TransferDirection.Incoming ? isTo : isFrom;
    }

    private static TransferDirection? InferTangerineTransferDirection(ImportedTransactionRow row)
    {
        var combined = $"{row.Description} {row.Notes}";

        if (
            combined.Contains("transfer from", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("deposit from", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("point from", StringComparison.OrdinalIgnoreCase)
        )
            return TransferDirection.Incoming;

        if (
            combined.Contains("transfer to", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("withdrawal to", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("point to", StringComparison.OrdinalIgnoreCase)
        )
            return TransferDirection.Outgoing;

        return null;
    }

    private static string? ExtractTangerineCounterpartyAccountNumber(ImportedTransactionRow row)
    {
        var combined = $"{row.Description} {row.Notes}";
        var match = Regex.Match(combined, @"(?<!\d)(\d{7,12})(?!\d)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ResolveCounterpartyAccountName(
        string? counterpartAccountNumber,
        Account destinationAccount,
        IReadOnlyList<Account> ownerAccounts
    )
    {
        if (string.IsNullOrWhiteSpace(counterpartAccountNumber))
            return null;

        return ownerAccounts
            .FirstOrDefault(account =>
                account.Id != destinationAccount.Id
                && string.Equals(
                    account.AccountNumber,
                    counterpartAccountNumber,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            ?.Name;
    }

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

    private static IReadOnlyDictionary<
        Domain.Enums.SecurityIdentifierType,
        string
    >? BuildSecurityIdentifiers(string? cusip, string? isin)
    {
        Dictionary<Domain.Enums.SecurityIdentifierType, string>? identifiers = null;

        if (!string.IsNullOrWhiteSpace(cusip))
        {
            identifiers ??= [];
            identifiers[Domain.Enums.SecurityIdentifierType.Cusip] = cusip.Trim();
        }

        if (!string.IsNullOrWhiteSpace(isin))
        {
            identifiers ??= [];
            identifiers[Domain.Enums.SecurityIdentifierType.Isin] = isin.Trim();
        }

        return identifiers;
    }

    private record ImportClassificationConfig(
        IReadOnlyList<string> BuyKeywords,
        IReadOnlyList<string> SellKeywords,
        IReadOnlyList<string> IncomeKeywords,
        IReadOnlyList<string> CashEquivalentSymbols
    );

    private record ImportedTransactionPresentation(
        TransactionType Type,
        string Description,
        Guid? CounterpartyAccountId = null,
        TransferDirection? Direction = null
    );

    private enum TransferDirection
    {
        Incoming,
        Outgoing,
    }
}
