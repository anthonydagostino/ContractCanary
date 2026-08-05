using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Awards;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class RecompeteRadarTests : ApiTestBase
{
    public RecompeteRadarTests(OppSignalWebAppFactory factory) : base(factory) { }

    private async Task<AwardIngestResult> RunAwardIngestAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAwardIngestService>();
        return await ingest.RunAsync();
    }

    private async Task SeedProfileAsync(Guid userId, string name, params string[] naics)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.MatchProfiles.Add(new MatchProfile
        {
            UserId = userId, Name = name, Naics = naics.ToList(),
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Recompetes_are_pro_gated()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"rrgate_{Guid.NewGuid():N}@test.dev");
        await SeedProfileAsync(userId, "IT", "541511");

        // Registration trial grants Starter → no Recompete Radar.
        var starter = await client.GetAsync("/api/recompetes");
        starter.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

        await SetPlanAsync(userId, PlanTier.Pro);
        var pro = await client.GetAsync("/api/recompetes");
        pro.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ingest_then_list_returns_matching_awards_soonest_first_with_prefix_matching()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"rrlist_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);
        // Watch a 4-digit prefix: fixture awards carry 6-digit leaves under it.
        await SeedProfileAsync(userId, "IT Services", "5415");

        (await RunAwardIngestAsync()).AwardsUpserted.Should().BeGreaterThan(0);

        var page = await client.GetFromJsonAsync<RecompetePage>("/api/recompetes");
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(i => i.NaicsCode!.StartsWith("5415"),
            "prefix codes must cover their subtree exactly like notice matching");
        page.Items.Should().OnlyContain(i => i.MatchedProfileNames.Contains("IT Services"));
        page.Items.Select(i => i.PeriodOfPerformanceEnd).Should().BeInAscendingOrder();
        page.Items.Should().OnlyContain(i => i.UsaSpendingUrl.StartsWith("https://www.usaspending.gov/award/"));

        // Awards outside every watched code never appear.
        page.Items.Should().OnlyContain(i => !i.NaicsCode!.StartsWith("23"));
    }

    [Fact]
    public async Task Award_ingest_is_idempotent()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"rridem_{Guid.NewGuid():N}@test.dev");
        await SeedProfileAsync(userId, "IT", "541511");

        await RunAwardIngestAsync();
        int countAfterFirst;
        using (var scope = Factory.Services.CreateScope())
            countAfterFirst = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Awards.CountAsync();

        await RunAwardIngestAsync();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Awards.CountAsync()).Should().Be(countAfterFirst, "re-running ingest must update, not duplicate");
        }
    }

    [Fact]
    public async Task One_failing_code_does_not_sink_the_rest_of_the_ingest()
    {
        // Also exercises the poisoned-tracker recovery: the failure path must
        // clear the change tracker so the next code's save isn't corrupted.
        var (_, _, userId) = await RegisterAndLoginAsync($"rrflaky_{Guid.NewGuid():N}@test.dev");
        await SeedProfileAsync(userId, "Mixed", "999999", "541511"); // 999999 throws in the stub

        using var factory = Factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<IAwardsClient, FlakyAwardsClient>()));
        using var scope = factory.Services.CreateScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAwardIngestService>();

        var result = await ingest.RunAsync();

        result.AwardsUpserted.Should().BeGreaterThan(0, "the healthy code must still ingest after the failing one");
        result.CodesFailed.Should().Be(1, "the failing code is reported, not hidden");
    }

    private sealed class FlakyAwardsClient : IAwardsClient
    {
        public string Source => "Flaky";

        public Task<IReadOnlyList<AwardRecord>> FetchExpiringAwardsAsync(
            string naicsCode, DateOnly endFrom, DateOnly endTo, CancellationToken ct = default)
        {
            if (naicsCode == "999999") throw new HttpRequestException("simulated outage");
            IReadOnlyList<AwardRecord> ok = new List<AwardRecord>
            {
                new(AwardKey: $"FLAKY_{naicsCode}", DisplayId: "F-1", RecipientName: "Recipient",
                    RecipientUei: null, AwardingAgency: "Agency", NaicsCode: naicsCode,
                    PscCode: null, ObligatedAmount: 1000m, PotentialTotalValue: null,
                    PeriodOfPerformanceStart: DateTime.UtcNow.AddYears(-4),
                    PeriodOfPerformanceEnd: DateTime.UtcNow.AddMonths(6),
                    PopState: null, RawJson: "{}"),
            };
            return Task.FromResult(ok);
        }
    }

    [Fact]
    public async Task A_poison_record_is_skipped_without_rolling_back_the_batch()
    {
        // An oversize key would DbUpdateException the whole code's batch —
        // and recur every run, permanently starving that code.
        var (_, _, userId) = await RegisterAndLoginAsync($"rrpoison_{Guid.NewGuid():N}@test.dev");
        await SeedProfileAsync(userId, "IT", "541511");

        using var factory = Factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddSingleton<IAwardsClient, MixedQualityAwardsClient>()));
        using var scope = factory.Services.CreateScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAwardIngestService>();

        var result = await ingest.RunAsync();

        result.AwardsUpserted.Should().Be(1, "the valid record persists; the poison one is skipped");
        result.CodesFailed.Should().Be(0);
    }

    private sealed class MixedQualityAwardsClient : IAwardsClient
    {
        public string Source => "MixedQuality";

        public Task<IReadOnlyList<AwardRecord>> FetchExpiringAwardsAsync(
            string naicsCode, DateOnly endFrom, DateOnly endTo, CancellationToken ct = default)
        {
            AwardRecord Make(string key) => new(
                AwardKey: key, DisplayId: "M-1", RecipientName: "Recipient", RecipientUei: null,
                AwardingAgency: "Agency", NaicsCode: naicsCode, PscCode: null,
                ObligatedAmount: 1000m, PotentialTotalValue: null,
                PeriodOfPerformanceStart: DateTime.UtcNow.AddYears(-4),
                PeriodOfPerformanceEnd: DateTime.UtcNow.AddMonths(6),
                PopState: null, RawJson: "{\"note\":\"contains \\u0000 escape\"}");

            IReadOnlyList<AwardRecord> records = new List<AwardRecord>
            {
                Make(new string('K', 200)), // oversize key → must be skipped
                Make($"GOOD_{naicsCode}"),
            };
            return Task.FromResult(records);
        }
    }

    [Fact]
    public async Task An_award_expiring_today_is_still_on_the_radar()
    {
        // PoP ends are stored as midnight UTC; a strict >= now comparison
        // dropped an award expiring today for the whole day.
        var (client, _, userId) = await RegisterAndLoginAsync($"rrtoday_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);
        await SeedProfileAsync(userId, "IT", "541511");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Awards.Add(new Award
            {
                AwardId = "ENDS_TODAY_1",
                RecipientName = "Last-Day Incumbent",
                NaicsCode = "541511",
                PeriodOfPerformanceEnd = DateTime.UtcNow.Date, // midnight UTC today
                RawJson = "{}",
                IngestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var page = await client.GetFromJsonAsync<RecompetePage>("/api/recompetes");
        page!.Items.Should().Contain(i => i.AwardId == "ENDS_TODAY_1");
    }

    [Fact]
    public async Task Long_expired_awards_are_pruned_on_ingest()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"rrprune_{Guid.NewGuid():N}@test.dev");
        await SeedProfileAsync(userId, "IT", "541511");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Awards.Add(new Award
            {
                AwardId = "STALE_AWARD_1",
                RecipientName = "Old Incumbent",
                NaicsCode = "541511",
                PeriodOfPerformanceEnd = DateTime.UtcNow.AddDays(-120),
                RawJson = "{}",
                IngestedAt = DateTime.UtcNow.AddDays(-200),
            });
            await db.SaveChangesAsync();
        }

        await RunAwardIngestAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Awards.AnyAsync(a => a.AwardId == "STALE_AWARD_1"))
                .Should().BeFalse("awards long past their recompete are dead weight");
        }
    }

    [Fact]
    public async Task Users_with_no_profiles_get_an_empty_page_not_an_error()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"rrempty_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);

        var page = await client.GetFromJsonAsync<RecompetePage>("/api/recompetes");
        page!.Items.Should().BeEmpty();
        page.Total.Should().Be(0);
    }
}
