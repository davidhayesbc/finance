using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.RollbackImport;

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
            transaction.SoftDelete();
        }

        batch.Status = ImportStatus.RolledBack;
        await _unitOfWork.ImportBatches.UpdateAsync(batch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
