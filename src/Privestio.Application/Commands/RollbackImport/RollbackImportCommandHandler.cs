using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.RollbackImport;

/// <summary>
/// Handles rollback of a previously imported batch by soft-deleting all of its transactions
/// and their split children.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Recovery path:</strong> Rollback followed by re-import is the intentional recovery
/// workflow. After rollback, existing fingerprints are no longer matched (soft-deleted rows are
/// excluded from duplicate detection), so re-importing the corrected file creates fresh records.
/// This is by-design and does not represent a data integrity gap.
/// </para>
/// <para>
/// Soft-deleted fingerprints are explicitly excluded by the global query filter and by the
/// <c>GetExistingFingerprintsAsync</c> query, ensuring the re-import proceeds cleanly.
/// </para>
/// </remarks>
public class RollbackImportCommandHandler : IRequestHandler<RollbackImportCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RollbackImportCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        RollbackImportCommand request,
        CancellationToken cancellationToken
    )
    {
        var batch = await _unitOfWork.ImportBatches.GetByIdAsync(
            request.ImportBatchId,
            cancellationToken
        );

        if (batch is null || batch.UserId != request.UserId)
            return false;

        if (batch.Status == ImportStatus.RolledBack)
            return false;

        // Soft-delete all transactions from this batch
        var transactions = await _unitOfWork.Transactions.GetByImportBatchIdAsync(
            request.ImportBatchId,
            cancellationToken
        );

        foreach (var transaction in transactions)
        {
            // Cascade soft-delete to split children before the parent
            foreach (var split in transaction.Splits.Where(s => !s.IsDeleted))
            {
                split.SoftDelete();
            }

            transaction.SoftDelete();
        }

        batch.Status = ImportStatus.RolledBack;
        await _unitOfWork.ImportBatches.UpdateAsync(batch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
