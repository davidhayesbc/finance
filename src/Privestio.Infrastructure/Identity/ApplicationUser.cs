using Microsoft.AspNetCore.Identity;

namespace Privestio.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user for authentication.
/// Linked to the domain User entity via IdentityUserId.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid DomainUserId { get; set; }
    public string BaseCurrency { get; set; } = "CAD";
    public string Locale { get; set; } = "en-CA";
}
