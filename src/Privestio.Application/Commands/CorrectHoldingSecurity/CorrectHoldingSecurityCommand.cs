using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CorrectHoldingSecurity;

public record CorrectHoldingSecurityCommand(
    Guid HoldingId,
    string Symbol,
    string? SecurityName,
    string? Source,
    string? Exchange,
    string? Cusip,
    string? Isin,
    Guid UserId
) : IRequest<HoldingResponse>;
