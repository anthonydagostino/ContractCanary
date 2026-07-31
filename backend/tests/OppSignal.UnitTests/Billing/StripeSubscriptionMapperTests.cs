using FluentAssertions;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Billing;
using Xunit;

namespace OppSignal.UnitTests.Billing;

public class StripeSubscriptionMapperTests
{
    [Theory]
    [InlineData("trialing", SubscriptionStatus.Trialing)]
    [InlineData("active", SubscriptionStatus.Active)]
    [InlineData("past_due", SubscriptionStatus.PastDue)]
    [InlineData("canceled", SubscriptionStatus.Canceled)]
    [InlineData("incomplete", SubscriptionStatus.Incomplete)]
    [InlineData("incomplete_expired", SubscriptionStatus.IncompleteExpired)]
    [InlineData("unpaid", SubscriptionStatus.Unpaid)]
    [InlineData("paused", SubscriptionStatus.Paused)]
    [InlineData("something_new", SubscriptionStatus.None)]
    [InlineData(null, SubscriptionStatus.None)]
    public void MapStatus_covers_all_stripe_statuses(string? stripe, SubscriptionStatus expected)
        => StripeSubscriptionMapper.MapStatus(stripe).Should().Be(expected);

    [Theory]
    [InlineData("price_pro", PlanTier.Pro)]
    [InlineData("price_starter", PlanTier.Starter)]
    [InlineData("price_unknown", PlanTier.None)]
    [InlineData(null, PlanTier.None)]
    public void MapPlan_resolves_by_price_id(string? priceId, PlanTier expected)
        => StripeSubscriptionMapper.MapPlan(priceId, "price_starter", "price_pro").Should().Be(expected);
}
