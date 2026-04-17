using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateTransfer;

public record CreateTransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    DateTime Date,
    Guid UserId,
    string? Notes = null
) : IRequest<TransferResponse>;
