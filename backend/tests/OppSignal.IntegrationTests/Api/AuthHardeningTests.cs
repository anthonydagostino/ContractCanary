using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Api.Infrastructure;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class AuthHardeningTests : ApiTestBase
{
    public AuthHardeningTests(OppSignalWebAppFactory factory) : base(factory) { }

    // ---- JwtKeyGuard (pure) ----------------------------------------------------

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("too-short", false)]
    [InlineData("dev-only-insecure-signing-key-change-me-in-prod-32b!", false)] // placeholder markers
    [InlineData("CHANGE_ME_to_a_long_random_secret_value_here", false)]
    [InlineData("a-genuinely-strong-random-looking-signing-key-9f3", true)]
    public void JwtKeyGuard_accepts_only_strong_non_placeholder_keys_in_production(string? key, bool ok)
        => (JwtKeyGuard.Evaluate(key, isDevelopment: false) is null).Should().Be(ok);

    [Fact]
    public void JwtKeyGuard_allows_any_nonempty_key_in_development_but_still_rejects_missing()
    {
        JwtKeyGuard.Evaluate("short-dev-key", isDevelopment: true).Should().BeNull();
        JwtKeyGuard.Evaluate("", isDevelopment: true).Should().NotBeNull();
    }

    // ---- Account lockout -------------------------------------------------------

    [Fact]
    public async Task Account_locks_after_repeated_failed_logins()
    {
        var email = $"lock_{Guid.NewGuid():N}@test.dev";
        await RegisterAndLoginAsync(email, "Password123!");
        var client = NewClient();

        for (var i = 0; i < 10; i++)
        {
            var bad = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });
            bad.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Even the CORRECT password is now refused with a lockout.
        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        locked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Refresh-token reuse detection ----------------------------------------

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_the_whole_family()
    {
        var email = $"reuse_{Guid.NewGuid():N}@test.dev";
        await RegisterAndLoginAsync(email);
        var client = NewClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var t1 = await login.Content.ReadFromJsonAsync<TokenResponse>();

        // Legit rotation: t1 -> t2, t1 now revoked.
        var rotate = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = t1!.RefreshToken });
        rotate.EnsureSuccessStatusCode();
        var t2 = await rotate.Content.ReadFromJsonAsync<TokenResponse>();

        // Reusing the OLD token is treated as theft: rejected AND the family is revoked.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = t1.RefreshToken });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // ...so the freshly-issued t2 is now invalid too.
        var afterReuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = t2!.RefreshToken });
        afterReuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_expired_refresh_token_is_rejected_without_revoking_other_sessions()
    {
        // Regression: mere expiry was treated like theft, so a dormant laptop
        // waking up (or anyone replaying a long-expired token) logged the user
        // out of every active device.
        var email = $"expiry_{Guid.NewGuid():N}@test.dev";
        await RegisterAndLoginAsync(email);
        var client = NewClient();

        var loginA = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var deviceA = await loginA.Content.ReadFromJsonAsync<TokenResponse>();
        var loginB = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var deviceB = await loginB.Content.ReadFromJsonAsync<TokenResponse>();

        // Device A goes dormant past the refresh-token lifetime.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hash = JwtTokenService.Hash(deviceA!.RefreshToken);
            var token = await db.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
            token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }

        // The expired token is rejected...
        var expired = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = deviceA!.RefreshToken });
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // ...but device B's active session survives — expiry is not reuse evidence.
        var refreshB = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = deviceB!.RefreshToken });
        refreshB.EnsureSuccessStatusCode();
    }

    // ---- Non-enumerating registration -----------------------------------------

    [Fact]
    public async Task Registering_an_existing_email_does_not_reveal_that_it_exists()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.dev";
        var client = NewClient();
        object Body() => new { email, password = "Password123!", fullName = "A", companyName = "B", timeZoneId = "America/New_York", acceptedTerms = true };

        (await client.PostAsJsonAsync("/api/auth/register", Body())).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", Body());
        second.StatusCode.Should().Be(HttpStatusCode.OK, "a duplicate signup must not return a distinguishable 409");
    }

    // ---- Authorization defaults -----------------------------------------------

    [Fact]
    public async Task Protected_endpoint_requires_authentication()
        => (await NewClient().GetAsync("/api/profiles")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Admin_endpoints_reject_a_non_admin_user()
    {
        var (client, _, _) = await RegisterAndLoginAsync($"nonadmin_{Guid.NewGuid():N}@test.dev");
        (await client.GetAsync("/api/admin/metrics")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
