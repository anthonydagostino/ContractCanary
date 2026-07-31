using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Email;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class DigestServiceTests : ApiTestBase
{
    public DigestServiceTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Digest_sends_once_marks_notified_and_is_not_resent()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"digest_{Guid.NewGuid():N}@test.dev");

        // Seed a notice + active profile + an un-notified match for this user.
        Guid profileId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notice = new Notice
            {
                NoticeId = $"DGT-{Guid.NewGuid():N}"[..16],
                Title = "IT Support Services",
                Type = NoticeType.CombinedSynopsis,
                NaicsCode = "541519",
                AgencyPath = "DEPT OF DEFENSE.DEPT OF THE ARMY",
                PostedDate = DateTime.UtcNow.Date,
                ResponseDeadline = DateTime.UtcNow.AddDays(20),
                PopState = "VA", PopCity = "Arlington",
                UiLink = "https://sam.gov/opp/x/view",
                FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
            };
            var profile = new MatchProfile
            {
                UserId = userId, Name = "IT", Naics = new() { "541519" },
                IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            };
            db.Notices.Add(notice);
            db.MatchProfiles.Add(profile);
            await db.SaveChangesAsync();
            db.NoticeMatches.Add(new NoticeMatch
            {
                NoticeId = notice.NoticeId, MatchProfileId = profile.Id, UserId = userId,
                MatchedAt = DateTime.UtcNow, NotifiedAt = null,
            });
            await db.SaveChangesAsync();
            profileId = profile.Id;
        }

        // First send: succeeds and marks the match notified.
        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeTrue();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.NoticeMatches.CountAsync(m => m.MatchProfileId == profileId && m.NotifiedAt != null))
                .Should().Be(1, "the match was included in a digest");
            (await db.EmailLogs.CountAsync(e => e.UserId == userId && e.Kind == EmailKind.Digest && e.Success))
                .Should().Be(1);
        }

        // Second send: nothing un-notified left → no email.
        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeFalse();
        }
    }
}
