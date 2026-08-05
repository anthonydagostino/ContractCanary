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

    private async Task<int> RunAwardIngestAsync()
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

        (await RunAwardIngestAsync()).Should().BeGreaterThan(0);

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
    public async Task Users_with_no_profiles_get_an_empty_page_not_an_error()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"rrempty_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);

        var page = await client.GetFromJsonAsync<RecompetePage>("/api/recompetes");
        page!.Items.Should().BeEmpty();
        page.Total.Should().Be(0);
    }
}
