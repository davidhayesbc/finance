using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class RecurringTransactionMapper
{
    public static RecurringTransactionResponse ToResponse(RecurringTransaction recurring) =>
        new()
        {
            Id = recurring.Id,
            AccountId = recurring.AccountId,
            Description = recurring.Description,
            Amount = recurring.Amount.Amount,
            Currency = recurring.Amount.CurrencyCode,
            TransactionType = recurring.TransactionType.ToString(),
            Frequency = recurring.Frequency.ToString(),
            StartDate = recurring.StartDate,
            EndDate = recurring.EndDate,
            NextOccurrence = recurring.NextOccurrence,
            LastGenerated = recurring.LastGenerated,
            CategoryId = recurring.CategoryId,
            CategoryName = recurring.Category?.Name,
            PayeeId = recurring.PayeeId,
            PayeeName = recurring.Payee?.DisplayName,
            IsActive = recurring.IsActive,
            Notes = recurring.Notes,
            CreatedAt = recurring.CreatedAt,
            UpdatedAt = recurring.UpdatedAt,
        };
}
