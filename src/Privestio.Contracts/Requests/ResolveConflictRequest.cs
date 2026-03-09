namespace Privestio.Contracts.Requests;

public record ResolveConflictRequest(
    Guid ConflictId,
    string Resolution, // KeepLocal, KeepServer, Merged
    string? MergedData = null
);
