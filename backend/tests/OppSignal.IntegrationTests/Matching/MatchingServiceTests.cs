using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Matching;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Matching;

public class MatchingServiceTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public MatchingServiceTests(PostgresTestDatabase pg) => _pg = pg;
    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private MatchingService Service(AppDbContext db) => new(db, new MatchEngine(), _clock);

    [Fact]
    public async Task Backfill_creates_notified_matches_over_matching_active_notices_only()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.AddRange(
            TestEntities.Notice("A", naics: "541512", active: true),
            TestEntities.Notice("B", naics: "541512", active: true),
            TestEntities.Notice("C", naics: "999999", active: true),   // wrong NAICS
            TestEntities.Notice("D", naics: "541512", active: false)); // inactive
        var profile = TestEntities.Profile(user, "IT", "541512");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();

        var created = await Service(db).BackfillProfileAsync(profile.Id, user);

        created.Should().Be(2);
        var matches = await db.NoticeMatches.AsNoTracking().ToListAsync();
        matches.Select(m => m.NoticeId).Should().BeEquivalentTo(new[] { "A", "B" });
        matches.Should().OnlyContain(m => m.NotifiedAt != null, "backfilled backlog is browsable, not re-emailed");
        matches.Should().OnlyContain(m => m.UserId == user && m.MatchReason != null);
    }

    [Fact]
    public async Task Backfill_across_batch_boundaries_with_tied_posted_dates_is_complete_and_duplicate_free()
    {
        // Regression: batches were ordered only by PostedDate (date-only, massively
        // tied) with no unique tiebreak — successive Skip/Take pages could repeat a
        // notice (unique-index violation → 500 after the profile row was already
        // saved) or drop one (silently missing matches).
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        var sharedPostedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 7; i++)
            db.Notices.Add(TestEntities.Notice($"TIED-{i}", naics: "541512", posted: sharedPostedDate));
        var profile = TestEntities.Profile(user, "IT", "541512");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();

        // batchSize 2 forces four pages over the seven tied rows.
        var created = await Service(db).BackfillProfileAsync(profile.Id, user, batchSize: 2, ct: default);

        created.Should().Be(7);
        var matchIds = await db.NoticeMatches.AsNoTracking().Select(m => m.NoticeId).ToListAsync();
        matchIds.Should().OnlyHaveUniqueItems();
        matchIds.Should().BeEquivalentTo(Enumerable.Range(0, 7).Select(i => $"TIED-{i}"));
    }

    [Fact]
    public async Task Backfill_replaces_stale_matches_when_the_profile_filter_changes()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.AddRange(
            TestEntities.Notice("A", naics: "541512", active: true),
            TestEntities.Notice("B", naics: "236220", active: true));
        var profile = TestEntities.Profile(user, "IT", "541512");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();
        var svc = Service(db);

        (await svc.BackfillProfileAsync(profile.Id, user)).Should().Be(1); // matches A

        profile.Naics = new() { "236220" };
        await db.SaveChangesAsync();
        (await svc.BackfillProfileAsync(profile.Id, user)).Should().Be(1); // now matches B

        var matches = await db.NoticeMatches.AsNoTracking().ToListAsync();
        matches.Select(m => m.NoticeId).Should().Equal("B");
    }

    [Fact]
    public async Task Backfill_of_an_inactive_profile_creates_nothing()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A", naics: "541512", active: true));
        var profile = TestEntities.Profile(user, "IT", "541512");
        profile.IsActive = false;
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();

        (await Service(db).BackfillProfileAsync(profile.Id, user)).Should().Be(0);
        (await db.NoticeMatches.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Backfill_ignores_a_profile_owned_by_another_user()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A", naics: "541512", active: true));
        var profile = TestEntities.Profile(owner, "IT", "541512");
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();
        var svc = Service(db);

        (await svc.BackfillProfileAsync(profile.Id, owner)).Should().Be(1);

        // Passing the owner's profile id with a different userId must be a no-op — no
        // rebuild and, crucially, no destructive delete of the owner's matches.
        (await svc.BackfillProfileAsync(profile.Id, attacker)).Should().Be(0);
        (await db.NoticeMatches.CountAsync(m => m.UserId == owner)).Should().Be(1);
    }

    [Fact]
    public async Task MatchNotices_creates_unnotified_matches_ready_for_the_next_digest()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        var profile = TestEntities.Profile(user, "IT", "541512");
        db.MatchProfiles.Add(profile);
        var notice = TestEntities.Notice("A", naics: "541512", active: true);
        db.Notices.Add(notice);
        await db.SaveChangesAsync();

        var created = await Service(db).MatchNoticesAsync(new[] { notice });

        created.Should().Be(1);
        var match = await db.NoticeMatches.AsNoTracking().SingleAsync();
        match.NotifiedAt.Should().BeNull("ingest-created matches are emailed in the next digest");
        match.UserId.Should().Be(user);
    }

    [Fact]
    public async Task MatchNotices_is_idempotent_and_does_not_duplicate()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.MatchProfiles.Add(TestEntities.Profile(user, "IT", "541512"));
        var notice = TestEntities.Notice("A", naics: "541512", active: true);
        db.Notices.Add(notice);
        await db.SaveChangesAsync();
        var svc = Service(db);

        (await svc.MatchNoticesAsync(new[] { notice })).Should().Be(1);
        (await svc.MatchNoticesAsync(new[] { notice })).Should().Be(0, "the match already exists");
        (await db.NoticeMatches.CountAsync()).Should().Be(1);
    }
}
