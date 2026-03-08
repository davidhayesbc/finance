using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateRecurringTransaction;

public class UpdateRecurringTransactionCommandHandler
    : IRequestHandler<UpdateRecurringTransactionCommand, RecurringTransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecurringTransactionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringTransactionResponse> Handle(
        UpdateRecurringTransactionCommand request,
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
                "Cannot update another user's recurring transaction."
            );

        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);
        var frequency = Enum.Parse<RecurrenceFrequency>(request.Frequency);

        recurring.UpdateDetails(
            request.Description,
            new Money(request.Amount, request.Currency),
            transactionType,
            frequency,
            request.CategoryId,
            request.PayeeId,
            request.Notes
        );

        recurring.UpdateSchedule(request.StartDate, request.EndDate);

        await _unitOfWork.RecurringTransactions.UpdateAsync(recurring, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RecurringTransactionMapper.ToResponse(recurring);
    }
}
