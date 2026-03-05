namespace Privestio.Domain.Entities;

/// <summary>
/// User-defined rule for automatic transaction categorization.
/// Rules are evaluated in priority order; first matching rule wins.
/// </summary>
public class CategorizationRule : BaseEntity
{
    private CategorizationRule() { }

    public CategorizationRule(
        string name,
        int priority,
        string conditions,
        string actions,
        Guid userId
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(conditions);
        ArgumentException.ThrowIfNullOrWhiteSpace(actions);

        Name = name.Trim();
        Priority = priority;
        Conditions = conditions;
        Actions = actions;
        UserId = userId;
        IsEnabled = true;
    }

    public string Name { get; private set; } = string.Empty;
    public int Priority { get; set; }

    /// <summary>
    /// JSON-serialized rule conditions (conforms to RulesEngine workflow format).
    /// </summary>
    public string Conditions { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized rule actions (e.g., set category, add tags, split).
    /// </summary>
    public string Actions { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
