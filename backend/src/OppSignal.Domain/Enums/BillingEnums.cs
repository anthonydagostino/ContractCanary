namespace OppSignal.Domain.Enums;

/// <summary>Subscription plan tier. Also the single source of ordering (None &lt; Starter &lt; Pro).</summary>
public enum PlanTier
{
    None = 0,
    Starter = 1,
    Pro = 2,
}

/// <summary>
/// Mirrors Stripe subscription status values so the app can gate features without
/// calling Stripe on every request.
/// </summary>
public enum SubscriptionStatus
{
    None = 0,
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Incomplete = 5,
    IncompleteExpired = 6,
    Unpaid = 7,
    Paused = 8,
}
