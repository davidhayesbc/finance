using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class TransactionMapper
{
    public static TransactionResponse ToResponse(Transaction transaction) =>
        new()
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Date = transaction.Date,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.CurrencyCode,
            Description = transaction.Description,
            TransactionType = transaction.Type.ToString(),
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            PayeeId = transaction.PayeeId,
            PayeeName = transaction.Payee?.DisplayName,
            IsReconciled = transaction.IsReconciled,
            IsSplit = transaction.IsSplit,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
        };
}
