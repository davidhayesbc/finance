using System.Security.Claims;

namespace Privestio.Api.Endpoints;

/// <summary>
/// Shared helper methods for API endpoint handlers.
/// </summary>
internal static class EndpointHelpers
{
    /// <summary>
    /// Extracts the domain user ID from the claims principal.
    /// Looks for the custom "domain_user_id" claim first, then falls back to NameIdentifier.
    /// </summary>
    internal static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("domain_user_id") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
