using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>
/// Local mirror of a user's Stripe subscription state (1:1 with a user). Kept in
/// sync by Stripe webhooks so feature gating never needs a live Stripe call.
/// </summary>
public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }

    public PlanTier Plan { get; set; } = PlanTier.None;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.None;

    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CurrentPeriodEndsAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Created-timestamp of the newest Stripe event applied to this row. Stripe
    /// retries and does not guarantee order; without this, a stale
    /// subscription.updated arriving after subscription.deleted would resurrect
    /// a canceled subscription locally.
    /// </summary>
    public DateTime? LastStripeEventAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
