using System.Globalization;
using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Pagination;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
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
        CancellationToken cancellationToken
    )
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
            request.SearchTerm,
            cancellationToken
        );

        var runningBalances = await ComputeRunningBalancesAsync(
            request,
            account,
            items,
            cancellationToken
        );

        var enrichedNextCursor = BuildNextCursorWithAnchor(nextCursor, items, runningBalances);

        return new PagedResponse<TransactionResponse>
        {
            Items = items
                .Select((t, i) => MapToResponse(t, runningBalances[i]))
                .ToList()
                .AsReadOnly(),
            PageSize = request.PageSize,
            NextCursor = enrichedNextCursor,
        };
    }

    private async Task<decimal[]> ComputeRunningBalancesAsync(
        GetTransactionsQuery request,
        Account account,
        IReadOnlyList<Transaction> items,
        CancellationToken cancellationToken
    )
    {
        if (items.Count == 0)
            return [];

        if (HasSparseFilters(request))
        {
            return await ComputeRunningBalancesForSparseSelectionAsync(
                request.AccountId,
                account.OpeningBalance.Amount,
                items,
                cancellationToken
            );
        }

        var newestAnchor =
            TryParseCursorAnchorBalance(request.Cursor)
            ?? await GetRunningBalanceForNewestAsync(
                request.AccountId,
                account.OpeningBalance.Amount,
                items[0],
                cancellationToken
            );

        return ComputeRunningBalancesFromNewestAnchor(items, newestAnchor);
    }

    private static bool HasSparseFilters(GetTransactionsQuery request) =>
        request.CategoryId.HasValue || !string.IsNullOrWhiteSpace(request.SearchTerm);

    private async Task<decimal> GetRunningBalanceForNewestAsync(
        Guid accountId,
        decimal openingBalance,
        Transaction newest,
        CancellationToken cancellationToken
    )
    {
        var signedUpToNewest = await _unitOfWork.Transactions.GetSignedSumUpToAsync(
            accountId,
            newest.Date,
            newest.Id,
            cancellationToken
        );

        return openingBalance + signedUpToNewest;
    }

    private async Task<decimal[]> ComputeRunningBalancesForSparseSelectionAsync(
        Guid accountId,
        decimal openingBalance,
        IReadOnlyList<Transaction> items,
        CancellationToken cancellationToken
    )
    {
        var balances = new decimal[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var signedUpToPoint = await _unitOfWork.Transactions.GetSignedSumUpToAsync(
                accountId,
                items[i].Date,
                items[i].Id,
                cancellationToken
            );

            balances[i] = openingBalance + signedUpToPoint;
        }

        return balances;
    }

    /// <summary>
    /// Computes per-row running balances for a contiguous newest-to-oldest page,
    /// given the balance after the first (newest) row.
    /// </summary>
    private static decimal[] ComputeRunningBalancesFromNewestAnchor(
        IReadOnlyList<Transaction> items,
        decimal newestRowRunningBalance
    )
    {
        var balances = new decimal[items.Count];
        var running = newestRowRunningBalance;

        for (var i = 0; i < items.Count; i++)
        {
            balances[i] = running;
            running -= GetSignedAmount(items[i]);
        }

        return balances;
    }

    private static string? BuildNextCursorWithAnchor(
        string? nextCursor,
        IReadOnlyList<Transaction> items,
        IReadOnlyList<decimal> runningBalances
    )
    {
        if (string.IsNullOrWhiteSpace(nextCursor) || items.Count == 0 || runningBalances.Count == 0)
            return nextCursor;

        var oldestIndex = items.Count - 1;
        var oldestRunningBalance = runningBalances[oldestIndex];
        var beforeOldest = oldestRunningBalance - GetSignedAmount(items[oldestIndex]);

        return $"{nextCursor}|{beforeOldest.ToString(CultureInfo.InvariantCulture)}";
    }

    private static decimal? TryParseCursorAnchorBalance(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        var parts = cursor.Split('|');
        if (parts.Length < 3)
            return null;

        return decimal.TryParse(
            parts[2],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var anchor
        )
            ? anchor
            : null;
    }

    private static decimal GetSignedAmount(Transaction transaction) =>
        transaction.Type == TransactionType.Debit
            ? -transaction.Amount.Amount
            : transaction.Amount.Amount;

    private static TransactionResponse MapToResponse(Transaction t, decimal runningBalance) =>
        new()
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
            RunningBalance = runningBalance,
        };
}
