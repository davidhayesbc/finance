using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.ContributeSinkingFund;

public record ContributeSinkingFundCommand(
    Guid SinkingFundId,
    Guid UserId,
    decimal Amount,
    string Currency = "CAD"
) : IRequest<SinkingFundResponse>;
