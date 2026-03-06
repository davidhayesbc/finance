using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateTransactionSplits;

public record UpdateTransactionSplitsCommand(
    Guid TransactionId,
    Guid UserId,
    IReadOnlyList<SplitLineInput> Splits
) : IRequest<IReadOnlyList<TransactionSplitResponse>?>;

public record SplitLineInput(
    decimal Amount,
    string Currency,
    Guid CategoryId,
    string? Notes = null,
    decimal? Percentage = null
);
