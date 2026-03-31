using System.Security.Cryptography;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a refresh token used for rotating JWT access tokens.
/// Tokens are single-use and form a chain via ReplacedByToken for rotation detection.
/// </summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, TimeSpan lifetime)
    {
        UserId = userId;
        Token = GenerateSecureToken();
        ExpiresAt = DateTime.UtcNow.Add(lifetime);
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByToken;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
