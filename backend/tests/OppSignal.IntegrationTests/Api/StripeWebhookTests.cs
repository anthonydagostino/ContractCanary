using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class StripeWebhookTests : ApiTestBase
{
    public StripeWebhookTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Subscription_updated_webhook_syncs_local_mirror_to_pro_active()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"webhook_{Guid.NewGuid():N}@test.dev");

        var periodEnd = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
        const string template =
            "{\"id\":\"evt_test\",\"object\":\"event\",\"api_version\":\"2024-06-20\"," +
            "\"created\":1700000000,\"request\":null,\"type\":\"customer.subscription.updated\"," +
            "\"data\":{\"object\":{" +
            "\"id\":\"sub_test123\",\"object\":\"subscription\",\"customer\":\"cus_test123\",\"status\":\"active\"," +
            "\"cancel_at_period_end\":false,\"current_period_end\":__PERIOD__,\"trial_end\":null," +
            "\"items\":{\"object\":\"list\",\"data\":[{\"id\":\"si_1\",\"object\":\"subscription_item\"," +
            "\"price\":{\"id\":\"__PRICE__\",\"object\":\"price\"}}]}," +
            "\"metadata\":{\"userId\":\"__USER__\"}" +
            "}}}";
        var payload = template
            .Replace("__PERIOD__", periodEnd.ToString())
            .Replace("__PRICE__", OppSignalWebAppFactory.ProPriceId)
            .Replace("__USER__", userId.ToString());

        var signature = SignPayload(payload, OppSignalWebAppFactory.WebhookSecret);

        var client = NewClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", signature);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.Subscriptions.FirstAsync(s => s.UserId == userId);
        sub.Plan.Should().Be(PlanTier.Pro);
        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.StripeSubscriptionId.Should().Be("sub_test123");
        sub.StripeCustomerId.Should().Be("cus_test123");
    }

    [Fact]
    public async Task Webhook_with_bad_signature_is_rejected()
    {
        var client = NewClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhook")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", "t=1,v1=deadbeef");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Build a Stripe-Signature header exactly as Stripe does: t=..,v1=hex(hmacsha256("t.payload")).</summary>
    [Fact]
    public async Task A_stale_out_of_order_event_cannot_resurrect_a_canceled_subscription()
    {
        // Regression: Stripe retries and does not guarantee order. A late-arriving
        // `updated(active)` snapshot older than the applied `deleted` used to
        // resurrect the subscription locally — free entitlements without payment.
        var (_, _, userId) = await RegisterAndLoginAsync($"stale_{Guid.NewGuid():N}@test.dev");

        var deleted = SubscriptionEventPayload(
            type: "customer.subscription.deleted", created: 1700000200, status: "canceled", userId: userId);
        (await PostWebhookAsync(deleted)).StatusCode.Should().Be(HttpStatusCode.OK);

        var staleActive = SubscriptionEventPayload(
            type: "customer.subscription.updated", created: 1700000100, status: "active", userId: userId);
        (await PostWebhookAsync(staleActive)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.Subscriptions.FirstAsync(s => s.UserId == userId);
        sub.Status.Should().Be(SubscriptionStatus.Canceled, "the older snapshot must not overwrite newer state");
    }

    private static string SubscriptionEventPayload(string type, long created, string status, Guid userId)
    {
        var periodEnd = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
        const string template =
            "{\"id\":\"evt_test\",\"object\":\"event\",\"api_version\":\"2024-06-20\"," +
            "\"created\":__CREATED__,\"request\":null,\"type\":\"__TYPE__\"," +
            "\"data\":{\"object\":{" +
            "\"id\":\"sub_test123\",\"object\":\"subscription\",\"customer\":\"cus_test123\",\"status\":\"__STATUS__\"," +
            "\"cancel_at_period_end\":false,\"current_period_end\":__PERIOD__,\"trial_end\":null," +
            "\"items\":{\"object\":\"list\",\"data\":[{\"id\":\"si_1\",\"object\":\"subscription_item\"," +
            "\"price\":{\"id\":\"__PRICE__\",\"object\":\"price\"}}]}," +
            "\"metadata\":{\"userId\":\"__USER__\"}" +
            "}}}";
        return template
            .Replace("__CREATED__", created.ToString())
            .Replace("__TYPE__", type)
            .Replace("__STATUS__", status)
            .Replace("__PERIOD__", periodEnd.ToString())
            .Replace("__PRICE__", OppSignalWebAppFactory.ProPriceId)
            .Replace("__USER__", userId.ToString());
    }

    private async Task<HttpResponseMessage> PostWebhookAsync(string payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", SignPayload(payload, OppSignalWebAppFactory.WebhookSecret));
        return await NewClient().SendAsync(request);
    }

    private static string SignPayload(string payload, string secret)
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{t}.{payload}";
        var hash = new HMACSHA256(Encoding.UTF8.GetBytes(secret))
            .ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={t},v1={hex}";
    }
}
