namespace OppSignal.Infrastructure.Identity;

/// <summary>
/// A rotating refresh token. Only the SHA-256 hash of the token is stored, so a
/// database leak does not yield usable tokens. Rotated on every refresh.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash (base64) of the opaque token value.</summary>
    public string TokenHash { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>Hash of the token that superseded this one (rotation chain).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
