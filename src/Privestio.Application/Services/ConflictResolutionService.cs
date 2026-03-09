using System.Text.Json;
using Privestio.Domain.Entities;

namespace Privestio.Application.Services;

public interface IConflictResolutionService
{
    List<string> DetectConflicts(string localJson, string serverJson);
    bool AutoResolve(SyncConflict conflict);
    void QueueForUserResolution(SyncConflict conflict);
}

public class ConflictResolutionService : IConflictResolutionService
{
    // Fields that contain financial data and must not be auto-resolved
    private static readonly HashSet<string> FinancialFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "amount",
        "balance",
        "price",
        "total",
        "budgetedAmount",
        "actualAmount",
        "statementBalance",
        "targetAmount",
        "currentAmount",
    };

    /// <summary>
    /// Compares the local and server JSON representations of an entity
    /// and returns a list of field names where conflicts exist.
    /// </summary>
    public List<string> DetectConflicts(string localJson, string serverJson)
    {
        var conflicts = new List<string>();

        try
        {
            using var localDoc = JsonDocument.Parse(localJson);
            using var serverDoc = JsonDocument.Parse(serverJson);

            var localRoot = localDoc.RootElement;
            var serverRoot = serverDoc.RootElement;

            if (
                localRoot.ValueKind != JsonValueKind.Object
                || serverRoot.ValueKind != JsonValueKind.Object
            )
                return conflicts;

            foreach (var localProp in localRoot.EnumerateObject())
            {
                if (serverRoot.TryGetProperty(localProp.Name, out var serverValue))
                {
                    if (localProp.Value.ToString() != serverValue.ToString())
                    {
                        conflicts.Add(localProp.Name);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If we can't parse, treat the whole thing as a conflict
            conflicts.Add("*");
        }

        return conflicts;
    }

    /// <summary>
    /// Attempts to automatically resolve the conflict.
    /// Returns true if auto-resolved, false if manual resolution is needed.
    /// Non-financial fields with non-overlapping changes can be auto-merged.
    /// </summary>
    public bool AutoResolve(SyncConflict conflict)
    {
        var conflictingFields = DetectConflicts(conflict.LocalData, conflict.ServerData);

        if (conflictingFields.Count == 0)
        {
            // No actual conflict -- keep server version
            conflict.Resolve("KeepServer");
            return true;
        }

        // If any financial field is in conflict, require manual resolution
        if (conflictingFields.Any(f => FinancialFields.Contains(f)))
        {
            return false;
        }

        // If the wildcard is present, we can't auto-resolve
        if (conflictingFields.Contains("*"))
        {
            return false;
        }

        // Non-financial, non-overlapping changes: auto-resolve with server-wins
        conflict.Resolve("KeepServer");
        return true;
    }

    /// <summary>
    /// Marks the conflict for user resolution by keeping it in Pending status.
    /// The conflict entity should already be persisted.
    /// </summary>
    public void QueueForUserResolution(SyncConflict conflict)
    {
        // The conflict stays in Pending status until the user resolves it.
        // No additional action needed; the conflict is already persisted.
    }
}
