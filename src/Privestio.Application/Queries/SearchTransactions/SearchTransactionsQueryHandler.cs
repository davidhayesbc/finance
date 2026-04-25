using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

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

        var linkedTransfersByTransactionId = await GetLinkedTransfersByTransactionIdAsync(
            transactions,
            cancellationToken
        );
        var accountNameById = await GetAccountNamesByIdAsync(
            transactions,
            linkedTransfersByTransactionId,
            cancellationToken
        );

        return transactions
            .Select(t => MapToResponse(t, linkedTransfersByTransactionId, accountNameById))
            .ToList();
    }

    private async Task<Dictionary<Guid, Transaction>> GetLinkedTransfersByTransactionIdAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken
    )
    {
        var linkedByTransactionId = new Dictionary<Guid, Transaction>();
        var linkedIds = transactions
            .Where(t => t.Type == TransactionType.Transfer && t.LinkedTransferId.HasValue)
            .Select(t => t.LinkedTransferId!.Value)
            .Distinct()
            .ToList();

        foreach (var linkedId in linkedIds)
        {
            var linked = await _unitOfWork.Transactions.GetByIdAsync(linkedId, cancellationToken);
            if (linked is not null)
                linkedByTransactionId[linked.Id] = linked;
        }

        return linkedByTransactionId;
    }

    private async Task<Dictionary<Guid, string>> GetAccountNamesByIdAsync(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<Guid, Transaction> linkedTransfersByTransactionId,
        CancellationToken cancellationToken
    )
    {
        var names = new Dictionary<Guid, string>();
        var requiredIds = transactions
            .Where(t => t.Type == TransactionType.Transfer)
            .Select(t => t.AccountId)
            .Concat(linkedTransfersByTransactionId.Values.Select(t => t.AccountId))
            .Distinct();

        foreach (var accountId in requiredIds)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is not null)
                names[account.Id] = account.Name;
        }

        return names;
    }

    private static (Guid? FromAccountId, string? FromAccountName, Guid? ToAccountId, string? ToAccountName) ResolveTransferRoute(
        Transaction transaction,
        IReadOnlyDictionary<Guid, Transaction> linkedTransfersByTransactionId,
        IReadOnlyDictionary<Guid, string> accountNameById
    )
    {
        if (transaction.Type != TransactionType.Transfer || !transaction.LinkedTransferId.HasValue)
            return (null, null, null, null);

        if (!linkedTransfersByTransactionId.TryGetValue(transaction.LinkedTransferId.Value, out var linked))
            return (null, null, null, null);

        var isIncoming = transaction.Description.StartsWith("Transfer from", StringComparison.OrdinalIgnoreCase);
        var isOutgoing = transaction.Description.StartsWith("Transfer to", StringComparison.OrdinalIgnoreCase);

        if (isIncoming)
        {
            return (
                linked.AccountId,
                accountNameById.GetValueOrDefault(linked.AccountId),
                transaction.AccountId,
                accountNameById.GetValueOrDefault(transaction.AccountId)
            );
        }

        if (isOutgoing)
        {
            return (
                transaction.AccountId,
                accountNameById.GetValueOrDefault(transaction.AccountId),
                linked.AccountId,
                accountNameById.GetValueOrDefault(linked.AccountId)
            );
        }

        return (null, null, null, null);
    }

    private static TransactionResponse MapToResponse(
        Transaction transaction,
        IReadOnlyDictionary<Guid, Transaction> linkedTransfersByTransactionId,
        IReadOnlyDictionary<Guid, string> accountNameById
    )
    {
        var (fromAccountId, fromAccountName, toAccountId, toAccountName) = ResolveTransferRoute(
            transaction,
            linkedTransfersByTransactionId,
            accountNameById
        );

        return new TransactionResponse
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Date = transaction.Date,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.CurrencyCode,
            Description = transaction.Description,
            TransactionType = transaction.Type.ToString(),
            FromAccountId = fromAccountId,
            FromAccountName = fromAccountName,
            ToAccountId = toAccountId,
            ToAccountName = toAccountName,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            PayeeId = transaction.PayeeId,
            PayeeName = transaction.Payee?.DisplayName,
            IsReconciled = transaction.IsReconciled,
            IsSplit = transaction.IsSplit,
            Splits = transaction
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
                .ToList(),
            Notes = transaction.Notes,
            SettlementDate = transaction.SettlementDate,
            ActivityType = transaction.ActivityType,
            ActivitySubType = transaction.ActivitySubType,
            Direction = transaction.Direction,
            Symbol = transaction.Symbol,
            SecurityName = transaction.SecurityName,
            Quantity = transaction.Quantity,
            UnitPrice = transaction.UnitPrice,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
        };
    }
}
