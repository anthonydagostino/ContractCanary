using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class AuthFlowTests : ApiTestBase
{
    public AuthFlowTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_then_login_is_blocked_until_email_confirmed()
    {
        var client = NewClient();
        var email = $"blocked_{Guid.NewGuid():N}@test.dev";

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!", fullName = "A", companyName = "B", timeZoneId = "America/New_York" });
        reg.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBlocked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        loginBlocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await ConfirmEmailDirectAsync(email);

        var loginOk = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        loginOk.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task New_user_gets_a_starter_trial()
    {
        var (client, _, _) = await RegisterAndLoginAsync($"trial_{Guid.NewGuid():N}@test.dev");
        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        me!.Plan.Should().Be("Starter");
        me.SubscriptionStatus.Should().Be("Trialing");
        me.Limits.MaxProfiles.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_rotates_and_old_token_is_invalidated()
    {
        var client = NewClient();
        var email = $"refresh_{Guid.NewGuid():N}@test.dev";
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!", fullName = "A", companyName = "B", timeZoneId = "America/New_York" });
        await ConfirmEmailDirectAsync(email);
        var login = await (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" }))
            .Content.ReadFromJsonAsync<TokenResponse>();

        var refresh1 = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login!.RefreshToken });
        refresh1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old refresh token can no longer be used (rotation).
        var reuseOld = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        reuseOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wrong_password_is_unauthorized()
    {
        var email = $"wrong_{Guid.NewGuid():N}@test.dev";
        await RegisterAndLoginAsync(email);
        var client = NewClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record MeResponse(string Plan, string SubscriptionStatus, LimitsResponse Limits);
    private sealed record LimitsResponse(int MaxProfiles, bool CanExportCsv);
}
