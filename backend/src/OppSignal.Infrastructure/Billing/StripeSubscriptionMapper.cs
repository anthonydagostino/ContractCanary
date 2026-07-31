using OppSignal.Domain.Enums;

namespace OppSignal.Infrastructure.Billing;

/// <summary>Pure mapping of Stripe status strings + price ids to our domain enums. Unit-tested.</summary>
public static class StripeSubscriptionMapper
{
    public static SubscriptionStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "trialing" => SubscriptionStatus.Trialing,
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "incomplete" => SubscriptionStatus.Incomplete,
        "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
        "unpaid" => SubscriptionStatus.Unpaid,
        "paused" => SubscriptionStatus.Paused,
        _ => SubscriptionStatus.None,
    };

    public static PlanTier MapPlan(string? priceId, string? starterPriceId, string? proPriceId)
    {
        if (!string.IsNullOrWhiteSpace(priceId))
        {
            if (priceId == proPriceId) return PlanTier.Pro;
            if (priceId == starterPriceId) return PlanTier.Starter;
        }
        return PlanTier.None;
    }
}
