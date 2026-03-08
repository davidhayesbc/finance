using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class BudgetMapper
{
    public static BudgetResponse ToResponse(Budget budget) =>
        new()
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category?.Name ?? string.Empty,
            Year = budget.Year,
            Month = budget.Month,
            Amount = budget.Amount.Amount,
            Currency = budget.Amount.CurrencyCode,
            RolloverEnabled = budget.RolloverEnabled,
            Notes = budget.Notes,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt,
        };
}
