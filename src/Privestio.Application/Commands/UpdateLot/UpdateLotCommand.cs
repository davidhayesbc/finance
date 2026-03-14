using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateLot;

public record UpdateLotCommand(
    Guid LotId,
    DateOnly AcquiredDate,
    decimal Quantity,
    decimal UnitCost,
    string Currency,
    Guid UserId,
    string? Notes = null
) : IRequest<LotResponse>;
