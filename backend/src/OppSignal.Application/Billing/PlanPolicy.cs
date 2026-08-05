using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Billing;

/// <summary>Feature limits for a plan tier.</summary>
public sealed record PlanLimits(int MaxProfiles, bool CanExportCsv, bool CanPrioritize, bool DailyDigest, bool CanSeeRecompetes);

/// <summary>
/// THE single source of truth for plan entitlements. Pure and exhaustively tested.
/// Both the API (enforcement) and the frontend (via a config endpoint) derive
/// limits from here.
/// </summary>
public static class PlanPolicy
{
    public const int TrialDays = 14;

    /// <summary>Grace window after a payment failure during which access is retained.</summary>
    public static readonly TimeSpan PastDueGrace = TimeSpan.FromDays(7);

    public static readonly PlanLimits None = new(MaxProfiles: 0, CanExportCsv: false, CanPrioritize: false, DailyDigest: false, CanSeeRecompetes: false);
    public static readonly PlanLimits Starter = new(MaxProfiles: 1, CanExportCsv: false, CanPrioritize: false, DailyDigest: true, CanSeeRecompetes: false);
    public static readonly PlanLimits Pro = new(MaxProfiles: 5, CanExportCsv: true, CanPrioritize: true, DailyDigest: true, CanSeeRecompetes: true);

    public static PlanLimits LimitsFor(PlanTier plan) => plan switch
    {
        PlanTier.Pro => Pro,
        PlanTier.Starter => Starter,
        _ => None,
    };

    /// <summary>
    /// Resolve the plan a user is currently ENTITLED to from their mirrored
    /// subscription state, without calling Stripe. Trials grant their plan until
    /// <c>TrialEndsAt</c>; past-due retains access within the grace window;
    /// canceled/expired grant nothing.
    /// </summary>
    public static PlanTier EffectivePlan(Subscription? sub, DateTime nowUtc)
    {
        if (sub is null) return PlanTier.None;

        return sub.Status switch
        {
            SubscriptionStatus.Active => sub.Plan,
            SubscriptionStatus.Trialing =>
                sub.TrialEndsAt is null || nowUtc <= sub.TrialEndsAt ? sub.Plan : PlanTier.None,
            SubscriptionStatus.PastDue =>
                sub.CurrentPeriodEndsAt is { } end && nowUtc <= end + PastDueGrace ? sub.Plan : PlanTier.None,
            _ => PlanTier.None,
        };
    }

    public static PlanLimits EffectiveLimits(Subscription? sub, DateTime nowUtc) =>
        LimitsFor(EffectivePlan(sub, nowUtc));
}
