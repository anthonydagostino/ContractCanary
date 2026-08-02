using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class ChangePasswordTests : ApiTestBase
{
    public ChangePasswordTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Change_password_updates_the_password_and_revokes_other_sessions()
    {
        var email = $"chpw_{Guid.NewGuid():N}@test.dev";
        var (client, _, _) = await RegisterAndLoginAsync(email, "OldPassword123!");

        // A second, independent session (its refresh token should be revoked by the change).
        var other = NewClient();
        var otherLogin = await other.PostAsJsonAsync("/api/auth/login", new { email, password = "OldPassword123!" });
        var otherTokens = await otherLogin.Content.ReadFromJsonAsync<TokenResponse>();

        var change = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "OldPassword123!", newPassword = "BrandNewPass456!" });
        change.StatusCode.Should().Be(HttpStatusCode.OK);
        var fresh = await change.Content.ReadFromJsonAsync<TokenResponse>();
        fresh!.AccessToken.Should().NotBeNullOrEmpty();

        // Old password no longer works; new one does.
        (await NewClient().PostAsJsonAsync("/api/auth/login", new { email, password = "OldPassword123!" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().PostAsJsonAsync("/api/auth/login", new { email, password = "BrandNewPass456!" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // The other session's refresh token was revoked.
        (await other.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = otherTokens!.RefreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_rejects_a_wrong_current_password()
    {
        var (client, _, _) = await RegisterAndLoginAsync($"chpw2_{Guid.NewGuid():N}@test.dev", "OldPassword123!");

        var change = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "NotMyPassword9!", newPassword = "BrandNewPass456!" });
        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Change_password_requires_authentication()
    {
        var resp = await NewClient().PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "x", newPassword = "BrandNewPass456!" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
