using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetRecurringTransactions;

public class GetRecurringTransactionsQueryHandler
    : IRequestHandler<GetRecurringTransactionsQuery, IReadOnlyList<RecurringTransactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRecurringTransactionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RecurringTransactionResponse>> Handle(
        GetRecurringTransactionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var recurrings = request.ActiveOnly
            ? await _unitOfWork.RecurringTransactions.GetActiveByUserIdAsync(
                request.UserId,
                cancellationToken
            )
            : await _unitOfWork.RecurringTransactions.GetByUserIdAsync(
                request.UserId,
                cancellationToken
            );

        return recurrings.Select(RecurringTransactionMapper.ToResponse).ToList().AsReadOnly();
    }
}
