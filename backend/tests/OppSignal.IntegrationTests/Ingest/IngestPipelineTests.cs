using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OppSignal.Application.Ingest;
using OppSignal.Application.Matching;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.IntegrationTests.Support;
using OppSignal.Infrastructure.Ingest;
using OppSignal.Infrastructure.Persistence;
using Xunit;

namespace OppSignal.IntegrationTests.Ingest;

public class IngestPipelineTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public IngestPipelineTests(PostgresTestDatabase pg) => _pg = pg;

    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private IngestService BuildIngest(AppDbContext db)
    {
        var engine = new MatchEngine();
        var matching = new MatchingService(db, engine, _clock);
        var client = new FixtureOpportunitiesClient(_clock);
        var options = Options.Create(new IngestOptions { Source = "Fixture", WindowDays = 45, PageSize = 200, MaxPagesPerRun = 25 });
        return new IngestService(db, client, matching, _clock, options, NullLogger<IngestService>.Instance);
    }

    private async Task<Guid> SeedItProfileAsync(AppDbContext db)
    {
        var profile = new MatchProfile
        {
            UserId = Guid.NewGuid(),
            Name = "IT services",
            Naics = new() { "541511", "541512", "541513", "541519", "518210" },
            NoticeTypes = new(),
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };
        db.MatchProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    [Fact]
    public async Task Ingest_inserts_notices_and_creates_matches()
    {
        await using var db = _pg.NewContext();
        await SeedItProfileAsync(db);

        var run = await BuildIngest(db).RunAsync(windowDaysOverride: 45);

        run.Status.Should().Be(IngestStatus.Succeeded);
        run.NoticesInserted.Should().BeGreaterThan(100, "fixture spreads 600 notices across 45 days");
        run.MatchesCreated.Should().BeGreaterThan(0);

        (await db.Notices.CountAsync()).Should().Be(run.NoticesInserted);
        var matches = await db.NoticeMatches.ToListAsync();
        matches.Should().OnlyContain(m => m.NotifiedAt == null, "ingest-created matches are emailable");
        matches.Should().OnlyContain(m => m.MatchReason != null);
    }

    [Fact]
    public async Task Ingest_is_idempotent_no_duplicate_notices_or_matches()
    {
        await using var db = _pg.NewContext();
        await SeedItProfileAsync(db);
        var ingest = BuildIngest(db);

        var first = await ingest.RunAsync(windowDaysOverride: 45);
        var noticeCount = await db.Notices.CountAsync();
        var matchCount = await db.NoticeMatches.CountAsync();

        // Second run over the same fixture window: everything already present.
        await using var db2 = _pg.NewContext();
        var second = await BuildIngest(db2).RunAsync(windowDaysOverride: 45);

        second.NoticesInserted.Should().Be(0);
        second.MatchesCreated.Should().Be(0);
        (await db2.Notices.CountAsync()).Should().Be(noticeCount);
        (await db2.NoticeMatches.CountAsync()).Should().Be(matchCount);

        first.NoticesInserted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Matches_only_created_for_matching_profile()
    {
        await using var db = _pg.NewContext();
        // A profile that should match nothing (bogus NAICS).
        db.MatchProfiles.Add(new MatchProfile
        {
            UserId = Guid.NewGuid(), Name = "Impossible", Naics = new() { "000000" },
            IsActive = true, CreatedAt = _clock.UtcNow, UpdatedAt = _clock.UtcNow,
        });
        await db.SaveChangesAsync();

        var run = await BuildIngest(db).RunAsync(windowDaysOverride: 45);

        run.NoticesInserted.Should().BeGreaterThan(0);
        run.MatchesCreated.Should().Be(0);
        (await db.NoticeMatches.CountAsync()).Should().Be(0);
    }
}
