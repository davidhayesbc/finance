using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.SearchTransactions;

public class SearchTransactionsQueryHandler
    : IRequestHandler<SearchTransactionsQuery, IReadOnlyList<TransactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchTransactionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TransactionResponse>> Handle(
        SearchTransactionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var transactions = await _unitOfWork.Transactions.SearchAsync(
            request.SearchTerm,
            request.OwnerId,
            request.MaxResults,
            cancellationToken
        );

        return transactions.Select(TransactionMapper.ToResponse).ToList();
    }
}
