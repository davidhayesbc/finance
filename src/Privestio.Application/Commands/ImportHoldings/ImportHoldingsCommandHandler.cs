using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.ImportHoldings;

public class ImportHoldingsCommandHandler
    : IRequestHandler<ImportHoldingsCommand, ImportHoldingsResultResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IHoldingsImporter> _importers;
    private readonly SecurityResolutionService _securityResolutionService;

    public ImportHoldingsCommandHandler(
        IUnitOfWork unitOfWork,
        IEnumerable<IHoldingsImporter> importers,
        SecurityResolutionService securityResolutionService
    )
    {
        _unitOfWork = unitOfWork;
        _importers = importers;
        _securityResolutionService = securityResolutionService;
    }

    public async Task<ImportHoldingsResultResponse> Handle(
        ImportHoldingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new KeyNotFoundException("Account not found.");
        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot import holdings into an account owned by another user."
            );

        var importer =
            _importers.FirstOrDefault(i => i.CanHandle(request.FileName))
            ?? throw new NotSupportedException(
                $"No holdings importer available for file: {request.FileName}"
            );

        var parseResult = await importer.ParseAsync(
            request.FileStream,
            request.FileName,
            cancellationToken
        );

        var statementDate = request.StatementDate ?? parseResult.StatementDate;

        var batch = new ImportBatch(request.FileName, importer.FileFormat, request.UserId);
        await _unitOfWork.ImportBatches.AddAsync(batch, cancellationToken);

        var createdCount = 0;
        var updatedCount = 0;
        var importedSecurityIds = new HashSet<Guid>();
        var importedPrices = new List<(Security Security, ImportedHoldingRow Row)>();
        var errors = parseResult
            .Errors.Select(e => new ImportErrorDetail
            {
                RowNumber = e.RowNumber,
                Message = e.ErrorMessage,
                RawData = e.RawData,
            })
            .ToList();

        foreach (var row in parseResult.Holdings)
        {
            var identifiers = BuildSecurityIdentifiers(row.Cusip, row.Isin);
            var security = await _securityResolutionService.ResolveOrCreateAsync(
                row.Symbol ?? SanitizeSymbol(row.InvestmentName),
                row.InvestmentName,
                parseResult.Currency,
                source: account.Institution ?? importer.FileFormat,
                exchange: row.Exchange,
                identifiers: identifiers,
                cancellationToken: cancellationToken
            );

            importedSecurityIds.Add(security.Id);
            importedPrices.Add((security, row));

            var existingHolding = await _unitOfWork.Holdings.GetByAccountIdAndSecurityIdAsync(
                request.AccountId,
                security.Id,
                cancellationToken
            );

            if (existingHolding is not null)
            {
                existingHolding.Update(row.Units, new Money(row.UnitPrice, parseResult.Currency));
                await _unitOfWork.Holdings.UpdateAsync(existingHolding, cancellationToken);
                updatedCount++;
            }
            else
            {
                var holding = new Holding(
                    request.AccountId,
                    security.Id,
                    security.DisplaySymbol,
                    row.InvestmentName,
                    row.Units,
                    new Money(row.UnitPrice, parseResult.Currency)
                );
                await _unitOfWork.Holdings.AddAsync(holding, cancellationToken);
                createdCount++;
            }
        }

        // Remove holdings that no longer appear in the latest statement (fund sold/consolidated)
        var existingHoldings = await _unitOfWork.Holdings.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        var removedCount = 0;
        foreach (var stale in existingHoldings)
        {
            if (!importedSecurityIds.Contains(stale.SecurityId))
            {
                await _unitOfWork.Holdings.DeleteAsync(stale.Id, cancellationToken);
                removedCount++;
            }
        }

        // Record unit prices from the statement as PriceHistory so the valuation service
        // uses statement prices instead of attempting Yahoo/MSN lookups for private fund symbols.
        if (statementDate != default)
        {
            // Remove any externally-fetched prices (Yahoo/MSN) for these securities so
            // PDFStatement prices are never overridden by a stale external quote.
            await _unitOfWork.PriceHistories.DeleteExternalPricesForSecuritiesAsync(
                importedSecurityIds,
                cancellationToken
            );

            var existingPriceKeys = await _unitOfWork.PriceHistories.GetExistingKeysAsync(
                importedPrices.Select(p => (p.Security.Id, statementDate)),
                cancellationToken
            );

            var newPrices = importedPrices
                .Where(p => !existingPriceKeys.Contains((p.Security.Id, statementDate)))
                .Select(p => new PriceHistory(
                    p.Security.Id,
                    p.Security.DisplaySymbol,
                    p.Security.DisplaySymbol,
                    new Money(p.Row.UnitPrice, parseResult.Currency),
                    statementDate,
                    "PDFStatement"
                ))
                .ToList();

            if (newPrices.Count > 0)
                await _unitOfWork.PriceHistories.AddRangeAsync(newPrices, cancellationToken);
        }

        batch.Complete(
            parseResult.Holdings.Count + parseResult.Errors.Count,
            createdCount + updatedCount,
            parseResult.Errors.Count,
            duplicateCount: 0
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportHoldingsResultResponse
        {
            ImportBatchId = batch.Id,
            StatementDate = statementDate,
            TotalHoldings = parseResult.Holdings.Count,
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            RemovedCount = removedCount,
            ErrorCount = parseResult.Errors.Count,
            Status = batch.Status.ToString(),
            Errors = errors,
        };
    }

    private static string SanitizeSymbol(string investmentName)
    {
        var parts = investmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = string.Concat(
            parts.Where(p => p.Length > 0 && char.IsUpper(p[0])).Select(p => p[0])
        );
        return initials.Length >= 2
            ? initials
            : investmentName[..Math.Min(10, investmentName.Length)].Trim();
    }

    private static IReadOnlyDictionary<SecurityIdentifierType, string>? BuildSecurityIdentifiers(
        string? cusip,
        string? isin
    )
    {
        Dictionary<SecurityIdentifierType, string>? identifiers = null;

        if (!string.IsNullOrWhiteSpace(cusip))
        {
            identifiers ??= [];
            identifiers[SecurityIdentifierType.Cusip] = cusip.Trim();
        }

        if (!string.IsNullOrWhiteSpace(isin))
        {
            identifiers ??= [];
            identifiers[SecurityIdentifierType.Isin] = isin.Trim();
        }

        return identifiers;
    }
}
