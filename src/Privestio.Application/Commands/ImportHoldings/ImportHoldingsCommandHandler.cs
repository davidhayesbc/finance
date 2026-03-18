using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
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
            var security = await _securityResolutionService.ResolveOrCreateAsync(
                row.Symbol ?? SanitizeSymbol(row.InvestmentName),
                row.InvestmentName,
                parseResult.Currency,
                cancellationToken: cancellationToken
            );

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
}
