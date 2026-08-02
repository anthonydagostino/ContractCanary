using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OppSignal.Infrastructure.Auth;

/// <summary>
/// Stateless, keyed (HMAC-SHA256) tokens for one-click email unsubscribe links, so a
/// recipient can opt out WITHOUT logging in (a CAN-SPAM requirement) and the link
/// can't be forged. Keyed off the JWT signing secret.
/// </summary>
public sealed class UnsubscribeTokenService
{
    private readonly byte[] _key;

    public UnsubscribeTokenService(IOptions<JwtOptions> jwt)
        => _key = Encoding.UTF8.GetBytes(jwt.Value.SigningKey);

    public string Create(Guid userId)
    {
        using var hmac = new HMACSHA256(_key);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes("unsubscribe:" + userId.ToString("N")));
        return Convert.ToBase64String(sig).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public bool Validate(Guid userId, string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        var expected = Create(userId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(token));
    }
}
