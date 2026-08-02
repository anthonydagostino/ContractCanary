using System.Text;

namespace OppSignal.Api.Infrastructure;

/// <summary>
/// Fail-fast validation of the JWT signing key. The key is the master secret of the
/// whole authorization model — a weak or placeholder key lets anyone forge an admin
/// token — so outside Development the app refuses to start unless a strong, real key
/// is configured. Pure and unit-tested.
/// </summary>
public static class JwtKeyGuard
{
    /// <summary>Minimum key length for HMAC-SHA256 (256-bit).</summary>
    public const int MinBytes = 32;

    // Substrings that mark a committed default / copy-me placeholder. Case-insensitive.
    private static readonly string[] PlaceholderMarkers =
    {
        "change", "insecure", "dev-only", "placeholder", "example", "your-secret", "changeme",
    };

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when the key must not be used.
    /// In Development any non-empty key is allowed (so contributors can run locally);
    /// elsewhere the key must be >= 32 bytes and not a known placeholder.
    /// </summary>
    public static void Validate(string? signingKey, bool isDevelopment)
    {
        var reason = Evaluate(signingKey, isDevelopment);
        if (reason is not null)
            throw new InvalidOperationException(
                $"Refusing to start: Jwt:SigningKey is {reason}. Set a strong random secret " +
                "(e.g. `openssl rand -base64 48`) via the JWT_SIGNING_KEY environment variable.");
    }

    /// <summary>Returns a human reason the key is unacceptable, or null if it is fine.</summary>
    public static string? Evaluate(string? signingKey, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
            return "missing";

        if (isDevelopment)
            return null; // any non-empty key is fine for local dev

        if (Encoding.UTF8.GetByteCount(signingKey) < MinBytes)
            return $"too short (needs >= {MinBytes} bytes)";

        foreach (var marker in PlaceholderMarkers)
            if (signingKey.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return "a known placeholder / default value";

        return null;
    }
}
