using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateHolding;

public record CreateHoldingCommand(
    Guid AccountId,
    string Symbol,
    string SecurityName,
    decimal Quantity,
    decimal AverageCostPerUnit,
    string Currency,
    Guid UserId,
    string? Notes = null
) : IRequest<HoldingResponse>;
