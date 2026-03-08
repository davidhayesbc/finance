using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.GenerateRecurringTransactions;

public class GenerateRecurringTransactionsCommandHandler
    : IRequestHandler<GenerateRecurringTransactionsCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GenerateRecurringTransactionsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TransactionResponse>> Handle(
        GenerateRecurringTransactionsCommand request,
        CancellationToken cancellationToken
    )
    {
        var activeRecurrings = await _unitOfWork.RecurringTransactions.GetActiveByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        var generated = new List<Transaction>();

        foreach (var recurring in activeRecurrings)
        {
            while (recurring.NextOccurrence <= request.UpToDate && recurring.IsActive)
            {
                var transaction = new Transaction(
                    recurring.AccountId,
                    recurring.NextOccurrence,
                    recurring.Amount,
                    recurring.Description,
                    recurring.TransactionType
                )
                {
                    CategoryId = recurring.CategoryId,
                    PayeeId = recurring.PayeeId,
                    Notes = recurring.Notes,
                };

                await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
                generated.Add(transaction);

                recurring.AdvanceToNextOccurrence();
            }

            await _unitOfWork.RecurringTransactions.UpdateAsync(recurring, cancellationToken);
        }

        if (generated.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return generated.Select(TransactionMapper.ToResponse).ToList().AsReadOnly();
    }
}
