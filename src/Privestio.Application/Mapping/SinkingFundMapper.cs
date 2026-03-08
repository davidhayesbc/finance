using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class SinkingFundMapper
{
    public static SinkingFundResponse ToResponse(SinkingFund fund) =>
        new()
        {
            Id = fund.Id,
            Name = fund.Name,
            TargetAmount = fund.TargetAmount.Amount,
            AccumulatedAmount = fund.AccumulatedAmount.Amount,
            MonthlySetAside = fund.CalculateMonthlySetAside(DateTime.UtcNow).Amount,
            ProgressPercentage = fund.ProgressPercentage,
            IsOnTrack = fund.IsOnTrack(DateTime.UtcNow),
            DueDate = fund.DueDate,
            Currency = fund.TargetAmount.CurrencyCode,
            AccountId = fund.AccountId,
            CategoryId = fund.CategoryId,
            CategoryName = fund.Category?.Name,
            IsActive = fund.IsActive,
            Notes = fund.Notes,
            CreatedAt = fund.CreatedAt,
            UpdatedAt = fund.UpdatedAt,
        };
}
