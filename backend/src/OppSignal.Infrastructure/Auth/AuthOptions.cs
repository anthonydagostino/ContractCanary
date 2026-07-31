namespace OppSignal.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "oppsignal";
    public string Audience { get; set; } = "oppsignal";

    /// <summary>HMAC signing key (>= 32 bytes). MUST be overridden in production via env.</summary>
    public string SigningKey { get; set; } = "dev-only-insecure-signing-key-change-me-32bytes!";

    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Require a confirmed email before login succeeds.</summary>
    public bool RequireConfirmedEmail { get; set; } = true;
}
