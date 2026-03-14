using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateLot;

public record CreateLotCommand(
    Guid HoldingId,
    DateOnly AcquiredDate,
    decimal Quantity,
    decimal UnitCost,
    string Currency,
    Guid UserId,
    string? Source = null,
    string? Notes = null
) : IRequest<LotResponse>;
