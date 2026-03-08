using MediatR;

namespace Privestio.Application.Commands.DeleteSinkingFund;

public record DeleteSinkingFundCommand(Guid SinkingFundId, Guid UserId) : IRequest;
