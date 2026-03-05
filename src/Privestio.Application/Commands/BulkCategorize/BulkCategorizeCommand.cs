using MediatR;

namespace Privestio.Application.Commands.BulkCategorize;

public record BulkCategorizeCommand(
    IReadOnlyList<Guid> TransactionIds,
    Guid CategoryId,
    Guid UserId
) : IRequest<int>;
