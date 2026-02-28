namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a normalized payee with aliases and a default category.
/// </summary>
public class Payee : BaseEntity
{
    private readonly List<string> _aliases = [];

    private Payee() { }

    public Payee(string displayName, Guid ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        OwnerId = ownerId;
    }

    public string DisplayName { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public User? Owner { get; set; }

    public Guid? DefaultCategoryId { get; set; }
    public Category? DefaultCategory { get; set; }

    public IReadOnlyCollection<string> Aliases => _aliases.AsReadOnly();

    public void AddAlias(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        var normalizedAlias = alias.Trim();
        if (!_aliases.Contains(normalizedAlias, StringComparer.OrdinalIgnoreCase))
        {
            _aliases.Add(normalizedAlias);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveAlias(string alias)
    {
        var existing = _aliases.FirstOrDefault(a =>
            a.Equals(alias.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _aliases.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool MatchesAlias(string rawPayee)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPayee);
        var trimmed = rawPayee.Trim();
        return _aliases.Any(a => a.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            || DisplayName.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
