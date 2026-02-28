namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a household grouping multiple users for shared finances.
/// </summary>
public class Household : BaseEntity
{
    private readonly List<User> _members = [];

    private Household() { }

    public Household(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<User> Members => _members.AsReadOnly();

    public void AddMember(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!_members.Any(m => m.Id == user.Id))
        {
            _members.Add(user);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveMember(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var existing = _members.FirstOrDefault(m => m.Id == user.Id);
        if (existing is not null)
        {
            _members.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
