using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.BulkCategorize;

public class BulkCategorizeCommandHandler : IRequestHandler<BulkCategorizeCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public BulkCategorizeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        BulkCategorizeCommand request,
        CancellationToken cancellationToken
    )
    {
        var updated = 0;

        foreach (var transactionId in request.TransactionIds)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(
                transactionId,
                cancellationToken
            );

            if (transaction is null)
                continue;

            transaction.CategoryId = request.CategoryId;
            await _unitOfWork.Transactions.UpdateAsync(transaction, cancellationToken);
            updated++;
        }

        if (updated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }
}
