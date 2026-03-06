using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateTransactionSplits;

public class UpdateTransactionSplitsCommandHandler
    : IRequestHandler<UpdateTransactionSplitsCommand, IReadOnlyList<TransactionSplitResponse>?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionSplitsCommandHandler(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<TransactionSplitResponse>?> Handle(
        UpdateTransactionSplitsCommand request,
        CancellationToken cancellationToken
    )
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(
            request.TransactionId,
            cancellationToken
        );
        if (transaction is null)
            return null;

        // Verify ownership
        var account = await _unitOfWork.Accounts.GetByIdAsync(
            transaction.AccountId,
            cancellationToken
        );
        if (account is null || account.OwnerId != request.UserId)
            return null;

        // Validate split sum matches transaction amount
        var splitSum = request.Splits.Sum(s => s.Amount);
        if (splitSum != transaction.Amount.Amount)
            return null;

        // Replace all splits
        transaction.ClearSplits();

        foreach (var splitInput in request.Splits)
        {
            var split = new TransactionSplit(
                transaction.Id,
                new Money(splitInput.Amount, splitInput.Currency),
                splitInput.CategoryId,
                splitInput.Notes,
                splitInput.Percentage
            );
            transaction.AddSplit(split);
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction
            .Splits.Select(s => new TransactionSplitResponse
            {
                Id = s.Id,
                TransactionId = s.TransactionId,
                Amount = s.Amount.Amount,
                Currency = s.Amount.CurrencyCode,
                CategoryId = s.CategoryId,
                CategoryName = s.Category?.Name,
                Notes = s.Notes,
                Percentage = s.Percentage,
            })
            .ToList();
    }
}
