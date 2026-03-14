using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateHolding;

public record UpdateHoldingCommand(
    Guid HoldingId,
    string SecurityName,
    decimal Quantity,
    decimal AverageCostPerUnit,
    string Currency,
    Guid UserId,
    string? Notes = null
) : IRequest<HoldingResponse>;
