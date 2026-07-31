using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OppSignal.Domain.Enums;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class PlanLimitEnforcementTests : ApiTestBase
{
    public PlanLimitEnforcementTests(OppSignalWebAppFactory factory) : base(factory) { }

    private static object Profile(string name) => new
    {
        name,
        naics = new[] { "541511" },
        psc = Array.Empty<string>(),
        keywords = Array.Empty<string>(),
        agencyPaths = Array.Empty<string>(),
        setAsides = Array.Empty<string>(),
        states = Array.Empty<string>(),
        noticeTypes = Array.Empty<string>(),
        isActive = true,
        isPriority = false,
    };

    [Fact]
    public async Task Starter_allows_one_profile_and_blocks_the_second()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"starter_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Starter);

        (await client.PostAsJsonAsync("/api/profiles", Profile("P1"))).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/profiles", Profile("P2"));
        second.StatusCode.Should().Be(HttpStatusCode.PaymentRequired); // 402 PlanLimit
    }

    [Fact]
    public async Task Pro_allows_five_profiles_and_blocks_the_sixth()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"pro_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);

        for (var i = 1; i <= 5; i++)
            (await client.PostAsJsonAsync("/api/profiles", Profile($"P{i}"))).StatusCode.Should().Be(HttpStatusCode.Created);

        var sixth = await client.PostAsJsonAsync("/api/profiles", Profile("P6"));
        sixth.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task Expired_trial_blocks_profile_creation()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"expired_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Starter, SubscriptionStatus.Canceled);

        var create = await client.PostAsJsonAsync("/api/profiles", Profile("P1"));
        create.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task Csv_export_is_gated_to_pro()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"csv_{Guid.NewGuid():N}@test.dev");

        await SetPlanAsync(userId, PlanTier.Starter);
        (await client.GetAsync("/api/notices/export.csv")).StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

        await SetPlanAsync(userId, PlanTier.Pro);
        var pro = await client.GetAsync("/api/notices/export.csv");
        pro.StatusCode.Should().Be(HttpStatusCode.OK);
        pro.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}
