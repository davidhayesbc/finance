using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteRecurringTransaction;

public class DeleteRecurringTransactionCommandHandler
    : IRequestHandler<DeleteRecurringTransactionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRecurringTransactionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        DeleteRecurringTransactionCommand request,
        CancellationToken cancellationToken
    )
    {
        var recurring =
            await _unitOfWork.RecurringTransactions.GetByIdAsync(
                request.RecurringTransactionId,
                cancellationToken
            )
            ?? throw new KeyNotFoundException(
                $"Recurring transaction {request.RecurringTransactionId} not found."
            );

        if (recurring.UserId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot delete another user's recurring transaction."
            );

        await _unitOfWork.RecurringTransactions.DeleteAsync(
            request.RecurringTransactionId,
            cancellationToken
        );
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
