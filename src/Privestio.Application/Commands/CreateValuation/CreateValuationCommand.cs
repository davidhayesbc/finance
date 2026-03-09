using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateValuation;

public record CreateValuationCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateOnly EffectiveDate,
    string Source,
    Guid UserId,
    string? Notes = null
) : IRequest<ValuationResponse>;
