using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Notices;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Notices;

public class NoticeStatsTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    // "now" is fixed so week/closing-soon boundaries are deterministic.
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public NoticeStatsTests(PostgresTestDatabase pg) => _pg = pg;
    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static DateTime D(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Stats_count_matched_active_new_this_week_closing_soon_and_saved()
    {
        var user = Guid.NewGuid();
        var now = _clock.UtcNow; // 2026-06-15 12:00; weekAgo = 06-08 12:00; soon = 06-22 12:00
        await using var db = _pg.NewContext();

        var m1 = TestEntities.Notice("M1", active: true, posted: D(2026, 6, 10), deadline: D(2026, 6, 20)); // this-week + closing-soon
        var m2 = TestEntities.Notice("M2", active: true, posted: D(2026, 6, 1), deadline: D(2026, 7, 30));  // matched-active only
        var m3 = TestEntities.Notice("M3", active: false, posted: D(2026, 6, 12), deadline: D(2026, 6, 19)); // inactive → excluded
        var m4 = TestEntities.Notice("M4", active: true, posted: D(2026, 6, 8, 12));                          // posted exactly at weekAgo boundary
        var u1 = TestEntities.Notice("U1", active: true, posted: D(2026, 6, 11));                             // active, not matched
        db.Notices.AddRange(m1, m2, m3, m4, u1);
        var profile = TestEntities.Profile(user, "P", "541512");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();

        foreach (var id in new[] { "M1", "M2", "M3", "M4" })
            db.NoticeMatches.Add(new NoticeMatch { NoticeId = id, MatchProfileId = profile.Id, UserId = user, MatchedAt = now, NotifiedAt = now });
        db.SavedNotices.Add(new SavedNotice { NoticeId = "M1", UserId = user, SavedAt = now });
        db.SavedNotices.Add(new SavedNotice { NoticeId = "U1", UserId = user, SavedAt = now });
        await db.SaveChangesAsync();

        var stats = await new NoticeService(db, _clock).GetStatsAsync(user);

        stats.MatchedActive.Should().Be(3, "M1, M2, M4 — M3 is inactive");
        stats.NewMatchesThisWeek.Should().Be(2, "M1 and M4 (posted on/after weekAgo)");
        stats.ClosingSoon.Should().Be(1, "only M1's deadline is within 7 days");
        stats.Saved.Should().Be(2);
        stats.TotalActive.Should().Be(4, "M1, M2, M4, U1 — M3 is inactive");
    }

    [Fact]
    public async Task Stats_are_zero_for_a_user_with_no_tracking_but_total_active_reflects_the_corpus()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.AddRange(TestEntities.Notice("A", active: true), TestEntities.Notice("B", active: false));
        await db.SaveChangesAsync();

        var stats = await new NoticeService(db, _clock).GetStatsAsync(user);

        stats.MatchedActive.Should().Be(0);
        stats.NewMatchesThisWeek.Should().Be(0);
        stats.ClosingSoon.Should().Be(0);
        stats.Saved.Should().Be(0);
        stats.TotalActive.Should().Be(1, "only A is active");
    }
}
