using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.ResolveConflict;

public record ResolveConflictCommand(
    Guid ConflictId,
    Guid UserId,
    string Resolution, // KeepLocal, KeepServer, Merged
    string? MergedData = null
) : IRequest<SyncConflictResponse>;
