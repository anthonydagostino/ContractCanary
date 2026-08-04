using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Email;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class CanSpamUnsubscribeTests : ApiTestBase
{
    public CanSpamUnsubscribeTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Opening_the_unsubscribe_link_does_not_opt_out_the_confirming_POST_does()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"unsub_{Guid.NewGuid():N}@test.dev");

        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<UnsubscribeTokenService>().Create(userId);

        // Regression: corporate mail scanners (Outlook SafeLinks, Mimecast)
        // prefetch every link in delivered mail with the REAL token. The GET
        // must only render the confirmation form — never change state.
        var page = await NewClient().GetAsync($"/api/unsubscribe?u={userId}&t={Uri.EscapeDataString(token)}");
        page.EnsureSuccessStatusCode();
        (await page.Content.ReadAsStringAsync()).Should().Contain("form", "the GET should render a confirm form");
        (await OptedOutAsync(userId)).Should().BeNull("a link prefetch must not unsubscribe the user");

        // Forged token on the POST: same page (no info leak), no change.
        (await PostUnsubscribeAsync(userId.ToString(), "forged")).EnsureSuccessStatusCode();
        (await OptedOutAsync(userId)).Should().BeNull();

        // Valid POST (the user clicking the confirm button): opts them out.
        (await PostUnsubscribeAsync(userId.ToString(), token)).EnsureSuccessStatusCode();
        (await OptedOutAsync(userId)).Should().NotBeNull();
    }

    private async Task<HttpResponseMessage> PostUnsubscribeAsync(string u, string t)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["u"] = u, ["t"] = t });
        return await NewClient().PostAsync("/api/unsubscribe", form);
    }

    [Fact]
    public async Task An_opted_out_user_is_skipped_even_when_they_have_matches()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"optout_{Guid.NewGuid():N}@test.dev");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(TestEntities.Notice("N1", posted: DateTime.UtcNow.Date));
            var profile = new MatchProfile { UserId = userId, Name = "P", Naics = new() { "541512" }, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.MatchProfiles.Add(profile);
            await db.SaveChangesAsync();
            db.NoticeMatches.Add(new NoticeMatch { NoticeId = "N1", MatchProfileId = profile.Id, UserId = userId, MatchedAt = DateTime.UtcNow, NotifiedAt = null });
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            user.DigestOptedOutAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeFalse("opted-out users must not receive the digest");
        }
    }

    private async Task<DateTime?> OptedOutAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.DigestOptedOutAt).FirstAsync();
    }
}
