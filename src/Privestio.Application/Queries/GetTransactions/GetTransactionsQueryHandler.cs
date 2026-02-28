using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Pagination;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Queries.GetTransactions;

public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PagedResponse<TransactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTransactionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<TransactionResponse>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        // Verify account ownership
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null || account.OwnerId != request.RequestingUserId)
        {
            return new PagedResponse<TransactionResponse>
            {
                Items = Array.Empty<TransactionResponse>(),
                PageSize = request.PageSize,
            };
        }

        DateRange? dateFilter = null;
        if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            dateFilter = new DateRange(request.FromDate.Value, request.ToDate.Value);
        }

        var (items, nextCursor) = await _unitOfWork.Transactions.GetPagedAsync(
            request.AccountId,
            request.PageSize,
            request.Cursor,
            dateFilter,
            request.CategoryId,
            cancellationToken);

        return new PagedResponse<TransactionResponse>
        {
            Items = items.Select(MapToResponse).ToList().AsReadOnly(),
            PageSize = request.PageSize,
            NextCursor = nextCursor,
        };
    }

    private static TransactionResponse MapToResponse(Transaction t) => new()
    {
        Id = t.Id,
        AccountId = t.AccountId,
        Date = t.Date,
        Amount = t.Amount.Amount,
        Currency = t.Amount.CurrencyCode,
        Description = t.Description,
        TransactionType = t.Type.ToString(),
        CategoryId = t.CategoryId,
        CategoryName = t.Category?.Name,
        PayeeId = t.PayeeId,
        PayeeName = t.Payee?.DisplayName,
        IsReconciled = t.IsReconciled,
        IsSplit = t.IsSplit,
        Notes = t.Notes,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };
}
