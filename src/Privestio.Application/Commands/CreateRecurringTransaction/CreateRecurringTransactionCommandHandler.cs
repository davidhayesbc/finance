using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateRecurringTransaction;

public class CreateRecurringTransactionCommandHandler
    : IRequestHandler<CreateRecurringTransactionCommand, RecurringTransactionResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecurringTransactionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringTransactionResponse> Handle(
        CreateRecurringTransactionCommand request,
        CancellationToken cancellationToken
    )
    {
        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);
        var frequency = Enum.Parse<RecurrenceFrequency>(request.Frequency);

        var recurring = new RecurringTransaction(
            request.UserId,
            request.AccountId,
            request.Description,
            new Money(request.Amount, request.Currency),
            transactionType,
            frequency,
            request.StartDate,
            request.EndDate,
            request.CategoryId,
            request.PayeeId,
            request.Notes
        );

        await _unitOfWork.RecurringTransactions.AddAsync(recurring, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RecurringTransactionMapper.ToResponse(recurring);
    }
}
