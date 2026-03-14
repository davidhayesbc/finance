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
