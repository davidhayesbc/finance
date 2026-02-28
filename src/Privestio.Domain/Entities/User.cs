namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a user of the application.
/// </summary>
public class User : BaseEntity
{
    private User() { }

    public User(string email, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Email = email.ToLowerInvariant().Trim();
        DisplayName = displayName.Trim();
    }

    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? IdentityUserId { get; set; }
    public string BaseCurrency { get; set; } = "CAD";
    public string Locale { get; set; } = "en-CA";
    public Guid? HouseholdId { get; set; }

    public Household? Household { get; set; }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Email = email.ToLowerInvariant().Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
