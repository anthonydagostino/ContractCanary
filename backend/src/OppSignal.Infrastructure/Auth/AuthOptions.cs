namespace OppSignal.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "oppsignal";
    public string Audience { get; set; } = "oppsignal";

    /// <summary>
    /// HMAC signing key (>= 32 bytes). MUST be set from a secret in production; the
    /// app refuses to start outside Development with a missing/placeholder key
    /// (see <c>JwtKeyGuard</c>). Left blank by default so a weak key can never ship.
    /// </summary>
    public string SigningKey { get; set; } = "";

    /// <summary>Short-lived access token. Kept small so logout/reset windows are tight.</summary>
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Require a confirmed email before login succeeds.</summary>
    public bool RequireConfirmedEmail { get; set; } = true;
}
