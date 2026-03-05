using MediatR;

namespace Privestio.Application.Commands.RollbackImport;

public record RollbackImportCommand(Guid ImportBatchId, Guid UserId) : IRequest<bool>;
