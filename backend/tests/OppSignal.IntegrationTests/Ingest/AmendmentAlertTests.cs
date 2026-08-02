using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OppSignal.Application.Ingest;
using OppSignal.Application.Matching;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Ingest;

/// <summary>
/// End-to-end coverage of the amendment/change-alert path in <see cref="IngestService"/>:
/// an update that materially changes a tracked notice raises a per-user NoticeAlert.
/// A stub client lets each test control exactly what the "upstream" returns.
/// </summary>
public class AmendmentAlertTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public AmendmentAlertTests(PostgresTestDatabase pg) => _pg = pg;
    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTime Aug1 = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private IngestService BuildIngest(AppDbContext db, ISamOpportunitiesClient client)
    {
        var matching = new MatchingService(db, new MatchEngine(), _clock);
        var options = Options.Create(new IngestOptions { Source = "Fixture", WindowDays = 90, PageSize = 200, MaxPagesPerRun = 5 });
        return new IngestService(db, client, matching, _clock, options, NullLogger<IngestService>.Instance);
    }

    private static SamOpportunityDto Dto(string id, string? deadlineIso, string active, string raw, string? naics = null)
        => new()
        {
            NoticeId = id,
            Title = $"Notice {id}",
            Type = "Solicitation",
            NaicsCode = naics,
            PostedDate = "2026-06-10",
            ResponseDeadLine = deadlineIso,
            Active = active,
            RawJson = raw,
        };

    private sealed class StubClient : ISamOpportunitiesClient
    {
        private readonly IReadOnlyList<SamOpportunityDto> _items;
        public StubClient(params SamOpportunityDto[] items) => _items = items;
        public IngestSource Source => IngestSource.Fixture;
        public Task<SamPage> FetchPageAsync(DateOnly from, DateOnly to, int limit, int offset, CancellationToken ct = default)
            => Task.FromResult(new SamPage(_items.Count, limit, offset,
                offset == 0 ? _items : (IReadOnlyList<SamOpportunityDto>)Array.Empty<SamOpportunityDto>()));
    }

    [Fact]
    public async Task Deadline_change_alerts_every_user_tracking_the_notice_and_no_one_else()
    {
        var matchedUser = Guid.NewGuid();
        var savedUser = Guid.NewGuid();

        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("X", deadline: Aug1, rawJson: "{\"v\":1}"));
        var profile = TestEntities.Profile(matchedUser, "P", "000000"); // NAICS deliberately does not auto-match
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();
        db.NoticeMatches.Add(new NoticeMatch { NoticeId = "X", MatchProfileId = profile.Id, UserId = matchedUser, MatchedAt = _clock.UtcNow, NotifiedAt = _clock.UtcNow });
        db.SavedNotices.Add(new SavedNotice { NoticeId = "X", UserId = savedUser, SavedAt = _clock.UtcNow });
        await db.SaveChangesAsync();

        var run = await BuildIngest(db, new StubClient(Dto("X", "2026-08-15T17:00:00Z", "Yes", "{\"v\":2}"))).RunAsync();

        run.Status.Should().Be(IngestStatus.Succeeded);
        run.NoticesUpdated.Should().Be(1);

        await using var check = _pg.NewContext();
        var alerts = await check.NoticeAlerts.ToListAsync();
        alerts.Should().HaveCount(2);
        alerts.Should().OnlyContain(a => a.Type == AlertType.DeadlineChanged && a.NoticeId == "X");
        alerts.Select(a => a.UserId).Should().BeEquivalentTo(new[] { matchedUser, savedUser });
    }

    [Fact]
    public async Task Cancellation_raises_a_cancelled_alert()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("Z", deadline: Aug1, rawJson: "{\"v\":1}"));
        await db.SaveChangesAsync();
        db.SavedNotices.Add(new SavedNotice { NoticeId = "Z", UserId = user, SavedAt = _clock.UtcNow });
        await db.SaveChangesAsync();

        await BuildIngest(db, new StubClient(Dto("Z", "2026-08-01T00:00:00Z", "No", "{\"v\":2}"))).RunAsync();

        await using var check = _pg.NewContext();
        var alerts = await check.NoticeAlerts.ToListAsync();
        alerts.Should().ContainSingle();
        alerts[0].Type.Should().Be(AlertType.Cancelled);
        alerts[0].UserId.Should().Be(user);
    }

    [Fact]
    public async Task An_immaterial_update_does_not_raise_alerts()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("Y", deadline: Aug1, rawJson: "{\"v\":1}"));
        await db.SaveChangesAsync();
        db.SavedNotices.Add(new SavedNotice { NoticeId = "Y", UserId = user, SavedAt = _clock.UtcNow });
        await db.SaveChangesAsync();

        // Same deadline, still active — only RawJson changed.
        var run = await BuildIngest(db, new StubClient(Dto("Y", "2026-08-01T00:00:00Z", "Yes", "{\"v\":2}"))).RunAsync();

        run.NoticesUpdated.Should().Be(1, "RawJson changed, so it is an update");
        await using var check = _pg.NewContext();
        (await check.NoticeAlerts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_brand_new_notice_never_raises_alerts()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.MatchProfiles.Add(TestEntities.Profile(user, "IT", "541512"));
        await db.SaveChangesAsync();

        var run = await BuildIngest(db, new StubClient(Dto("W", "2026-08-15T17:00:00Z", "Yes", "{\"v\":1}", naics: "541512"))).RunAsync();

        run.NoticesInserted.Should().Be(1);
        run.MatchesCreated.Should().Be(1, "the profile NAICS matches the new notice");
        await using var check = _pg.NewContext();
        (await check.NoticeAlerts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task A_user_tracking_by_both_match_and_save_gets_one_alert_not_two()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("D", deadline: Aug1, rawJson: "{\"v\":1}"));
        var profile = TestEntities.Profile(user, "P", "000000");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();
        db.NoticeMatches.Add(new NoticeMatch { NoticeId = "D", MatchProfileId = profile.Id, UserId = user, MatchedAt = _clock.UtcNow, NotifiedAt = _clock.UtcNow });
        db.SavedNotices.Add(new SavedNotice { NoticeId = "D", UserId = user, SavedAt = _clock.UtcNow });
        await db.SaveChangesAsync();

        await BuildIngest(db, new StubClient(Dto("D", "2026-08-20T12:00:00Z", "Yes", "{\"v\":2}"))).RunAsync();

        await using var check = _pg.NewContext();
        (await check.NoticeAlerts.CountAsync(a => a.UserId == user)).Should().Be(1);
    }
}
