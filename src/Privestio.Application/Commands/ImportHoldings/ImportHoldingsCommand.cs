using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.ImportHoldings;

public record ImportHoldingsCommand(
    Stream FileStream,
    string FileName,
    Guid AccountId,
    Guid UserId,
    DateOnly? StatementDate = null
) : IRequest<ImportHoldingsResultResponse>;
