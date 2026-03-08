using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateSinkingFund;

public record UpdateSinkingFundCommand(
    Guid SinkingFundId,
    Guid UserId,
    string Name,
    decimal TargetAmount,
    DateTime DueDate,
    string Currency = "CAD",
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Notes = null
) : IRequest<SinkingFundResponse>;
