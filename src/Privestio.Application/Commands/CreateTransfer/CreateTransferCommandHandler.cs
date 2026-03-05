using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateTransfer;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, TransferResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransferCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferResponse> Handle(
        CreateTransferCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        if (request.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.Amount),
                "Amount must be positive."
            );

        var amount = new Money(request.Amount, request.Currency);

        // Source: debit (money leaving)
        var sourceTransaction = new Transaction(
            request.SourceAccountId,
            request.Date,
            amount.Negate(),
            $"Transfer to account",
            TransactionType.Transfer
        )
        {
            Notes = request.Notes,
        };

        // Destination: credit (money arriving)
        var destinationTransaction = new Transaction(
            request.DestinationAccountId,
            request.Date,
            amount,
            $"Transfer from account",
            TransactionType.Transfer
        )
        {
            Notes = request.Notes,
        };

        // Link the two transactions
        sourceTransaction.LinkedTransferId = destinationTransaction.Id;
        destinationTransaction.LinkedTransferId = sourceTransaction.Id;

        await _unitOfWork.Transactions.AddAsync(sourceTransaction, cancellationToken);
        await _unitOfWork.Transactions.AddAsync(destinationTransaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TransferResponse
        {
            SourceTransactionId = sourceTransaction.Id,
            DestinationTransactionId = destinationTransaction.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            Date = request.Date,
        };
    }
}
