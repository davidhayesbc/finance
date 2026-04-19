namespace Privestio.Domain.Enums;

/// <summary>
/// Defines the role a member holds within a household.
/// </summary>
public enum HouseholdRole
{
    /// <summary>The original creator of the household. Cannot be removed or changed.</summary>
    Owner = 0,

    /// <summary>Can manage members (invite/remove) and rename the household.</summary>
    Admin = 1,

    /// <summary>Can view and edit all shared accounts and transactions.</summary>
    Member = 2,

    /// <summary>Read-only access to shared accounts. Cannot create or edit transactions.</summary>
    Viewer = 3,
}
