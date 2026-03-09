using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class ContributionRoomMapper
{
    public static ContributionRoomResponse ToResponse(ContributionRoom room) =>
        new()
        {
            Id = room.Id,
            AccountId = room.AccountId,
            AccountName = room.Account?.Name ?? string.Empty,
            Year = room.Year,
            AnnualLimitAmount = room.AnnualLimit.Amount,
            CarryForwardAmount = room.CarryForwardRoom.Amount,
            ContributionsYtdAmount = room.ContributionsYtd.Amount,
            RemainingRoomAmount = room.RemainingRoom.Amount,
            Currency = room.AnnualLimit.CurrencyCode,
        };
}
