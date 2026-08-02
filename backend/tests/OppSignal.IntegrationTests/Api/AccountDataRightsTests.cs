using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Identity;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class AccountDataRightsTests : ApiTestBase
{
    public AccountDataRightsTests(OppSignalWebAppFactory factory) : base(factory) { }

    private static HttpRequestMessage Delete(string password) =>
        new(HttpMethod.Delete, "/api/auth/me") { Content = JsonContent.Create(new { password }) };

    [Fact]
    public async Task Delete_account_requires_the_password_then_purges_everything()
    {
        var email = $"del_{Guid.NewGuid():N}@test.dev";
        var (client, _, userId) = await RegisterAndLoginAsync(email, "Password123!");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(TestEntities.Notice("N1"));
            db.MatchProfiles.Add(new MatchProfile { UserId = userId, Name = "P", Naics = new() { "541512" }, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            db.SavedNotices.Add(new SavedNotice { UserId = userId, NoticeId = "N1", SavedAt = DateTime.UtcNow });
            db.NoticeAlerts.Add(new NoticeAlert { UserId = userId, NoticeId = "N1", Type = AlertType.Cancelled, Message = "x", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        // Wrong password is refused.
        (await client.SendAsync(Delete("WrongPass9!"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Correct password deletes.
        (await client.SendAsync(Delete("Password123!"))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.MatchProfiles.CountAsync(p => p.UserId == userId)).Should().Be(0);
            (await db.SavedNotices.CountAsync(s => s.UserId == userId)).Should().Be(0);
            (await db.NoticeAlerts.CountAsync(a => a.UserId == userId)).Should().Be(0);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            (await users.FindByEmailAsync(email)).Should().BeNull();
        }

        // The account no longer exists, so login fails.
        (await NewClient().PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_returns_the_callers_own_data()
    {
        var email = $"exp_{Guid.NewGuid():N}@test.dev";
        var (client, _, userId) = await RegisterAndLoginAsync(email);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.MatchProfiles.Add(new MatchProfile { UserId = userId, Name = "Export Me", Naics = new() { "541512" }, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var resp = await client.GetAsync("/api/auth/me/export");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain(email);
        json.Should().Contain("Export Me");
    }

    [Fact]
    public async Task Data_rights_endpoints_require_authentication()
    {
        (await NewClient().GetAsync("/api/auth/me/export")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().SendAsync(Delete("x"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
