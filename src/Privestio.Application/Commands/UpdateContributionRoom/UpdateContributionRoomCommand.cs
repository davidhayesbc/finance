using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateContributionRoom;

public record UpdateContributionRoomCommand(
    Guid AccountId,
    Guid UserId,
    int Year,
    decimal? AnnualLimitAmount,
    decimal? CarryForwardAmount,
    decimal? ContributionAmount,
    string Currency
) : IRequest<ContributionRoomResponse>;
