using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Email;
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

    [Fact]
    public async Task A_failed_send_leaves_matches_unnotified_so_they_are_retried()
    {
        // Regression: SendDigestAsync returned "digest was non-empty", not "send
        // succeeded", so a provider outage at the send hour permanently stamped
        // every pending match/alert as notified — notifications silently lost.
        var (_, _, userId) = await RegisterAndLoginAsync($"failsend_{Guid.NewGuid():N}@test.dev");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var noticeId = $"FLS-{Guid.NewGuid():N}"[..16];
            db.Notices.Add(TestEntities.Notice(noticeId, naics: "541519", posted: DateTime.UtcNow.Date));
            var profile = new MatchProfile
            {
                UserId = userId, Name = "IT", Naics = new() { "541519" },
                IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            };
            db.MatchProfiles.Add(profile);
            await db.SaveChangesAsync();
            db.NoticeMatches.Add(new NoticeMatch
            {
                NoticeId = noticeId, MatchProfileId = profile.Id, UserId = userId,
                MatchedAt = DateTime.UtcNow, NotifiedAt = null,
            });
            await db.SaveChangesAsync();
        }

        // Same app, same database — but the email transport always fails.
        using var failingFactory = Factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddScoped<IEmailSender, AlwaysFailingEmailSender>()));

        using (var scope = failingFactory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeFalse("the transport failed");
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.NoticeMatches.CountAsync(m => m.UserId == userId && m.NotifiedAt == null))
                .Should().Be(1, "a failed send must leave the match pending for the next digest");
            (await db.EmailLogs.CountAsync(e => e.UserId == userId && e.Kind == EmailKind.Digest && !e.Success))
                .Should().Be(1, "the failure is still audited");
        }

        // Once the provider recovers, the pending match goes out.
        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeTrue("the match survived the outage");
        }
    }

    private sealed class AlwaysFailingEmailSender : IEmailSender
    {
        public string Provider => "FailingTest";
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
            => Task.FromResult(new EmailSendResult(false, Provider, null, "simulated provider outage"));
    }

    [Fact]
    public async Task Digest_sends_a_deadline_reminder_for_a_tracked_opportunity_closing_in_7_days()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"closing_{Guid.NewGuid():N}@test.dev");

        // Deadline exactly 7 local days out (test users register as America/New_York),
        // computed in local time so the 7/3/1-day bucketing is deterministic.
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var deadlineLocal = nowLocal.Date.AddDays(7).AddHours(12);
        var deadlineUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(deadlineLocal, DateTimeKind.Unspecified), tz);

        var noticeId = $"CLZ-{Guid.NewGuid():N}"[..16];
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(new Notice
            {
                NoticeId = noticeId,
                Title = "Perimeter Fencing Installation",
                Type = NoticeType.Solicitation,
                NaicsCode = "238990",
                AgencyPath = "DEPT OF DEFENSE",
                PostedDate = DateTime.UtcNow.Date.AddDays(-10),
                ResponseDeadline = deadlineUtc,
                FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
            });
            var profile = new MatchProfile
            {
                UserId = userId, Name = "Fencing", Naics = new() { "238990" },
                IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            };
            db.MatchProfiles.Add(profile);
            await db.SaveChangesAsync();
            // Already-notified match → NOT a "new" match, so the ONLY thing that can
            // trigger a digest is the closing-soon deadline reminder.
            db.NoticeMatches.Add(new NoticeMatch
            {
                NoticeId = noticeId, MatchProfileId = profile.Id, UserId = userId,
                MatchedAt = DateTime.UtcNow, NotifiedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId))
                .Should().BeTrue("a tracked opportunity 7 days from its deadline is a reminder-worthy digest");
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.EmailLogs.CountAsync(e => e.UserId == userId && e.Kind == EmailKind.Digest && e.Success))
                .Should().Be(1);
        }
    }

    [Fact]
    public async Task SendDueDigests_includes_a_user_whose_only_content_is_a_closing_soon_reminder()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"due_{Guid.NewGuid():N}@test.dev");

        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var deadlineLocal = nowLocal.Date.AddDays(3).AddHours(12);
        var deadlineUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(deadlineLocal, DateTimeKind.Unspecified), tz);

        var noticeId = $"DUE-{Guid.NewGuid():N}"[..16];
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(new Notice
            {
                NoticeId = noticeId, Title = "Runway Repair", Type = NoticeType.Solicitation,
                PostedDate = DateTime.UtcNow.Date.AddDays(-20), ResponseDeadline = deadlineUtc,
                FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
            });
            // Saved (tracked) but NO matches at all → the deadline reminder is the only content.
            db.SavedNotices.Add(new SavedNotice { UserId = userId, NoticeId = noticeId, SavedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        int sent;
        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            sent = await digest.SendDueDigestsAsync(nowLocal.Hour);
        }

        sent.Should().BeGreaterThanOrEqualTo(1, "the saved opportunity is 3 days from its deadline");
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.EmailLogs.CountAsync(e => e.UserId == userId && e.Kind == EmailKind.Digest && e.Success))
                .Should().Be(1);
        }
    }

    [Fact]
    public async Task Digest_sends_on_change_alert_alone_and_stamps_it_notified()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"digestalert_{Guid.NewGuid():N}@test.dev");

        // Seed a notice + an un-notified change-alert for this user (NO new matches).
        var noticeId = $"ALR-{Guid.NewGuid():N}"[..16];
        Guid alertId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(new Notice
            {
                NoticeId = noticeId,
                Title = "Base Facilities Maintenance",
                Type = NoticeType.Solicitation,
                PostedDate = DateTime.UtcNow.Date,
                FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
            });
            var alert = new NoticeAlert
            {
                UserId = userId,
                NoticeId = noticeId,
                Type = AlertType.DeadlineChanged,
                Message = "Response deadline moved from Jul 1 to Jul 15, 2026.",
                CreatedAt = DateTime.UtcNow,
            };
            db.NoticeAlerts.Add(alert);
            await db.SaveChangesAsync();
            alertId = alert.Id;
        }

        // Sends even with zero new matches, because there's an un-notified change-alert.
        using (var scope = Factory.Services.CreateScope())
        {
            var digest = scope.ServiceProvider.GetRequiredService<IDigestService>();
            (await digest.SendUserDigestAsync(userId)).Should().BeTrue();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.NoticeAlerts.Where(a => a.Id == alertId).Select(a => a.NotifiedAt).FirstAsync())
                .Should().NotBeNull("the alert was included in the digest");
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
