using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateSinkingFund;

public record CreateSinkingFundCommand(
    Guid UserId,
    string Name,
    decimal TargetAmount,
    DateTime DueDate,
    string Currency = "CAD",
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Notes = null
) : IRequest<SinkingFundResponse>;
