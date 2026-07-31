using FluentAssertions;
using OppSignal.Application.Billing;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using Xunit;

namespace OppSignal.UnitTests.Billing;

public class PlanPolicyTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    // ---- limits table -------------------------------------------------------

    [Fact]
    public void Starter_allows_one_profile_no_export_no_priority()
    {
        var l = PlanPolicy.LimitsFor(PlanTier.Starter);
        l.MaxProfiles.Should().Be(1);
        l.CanExportCsv.Should().BeFalse();
        l.CanPrioritize.Should().BeFalse();
        l.DailyDigest.Should().BeTrue();
    }

    [Fact]
    public void Pro_allows_five_profiles_export_and_priority()
    {
        var l = PlanPolicy.LimitsFor(PlanTier.Pro);
        l.MaxProfiles.Should().Be(5);
        l.CanExportCsv.Should().BeTrue();
        l.CanPrioritize.Should().BeTrue();
        l.DailyDigest.Should().BeTrue();
    }

    [Fact]
    public void None_allows_nothing()
    {
        var l = PlanPolicy.LimitsFor(PlanTier.None);
        l.MaxProfiles.Should().Be(0);
        l.CanExportCsv.Should().BeFalse();
        l.DailyDigest.Should().BeFalse();
    }

    // ---- effective plan resolution -----------------------------------------

    [Fact]
    public void Null_subscription_is_none()
        => PlanPolicy.EffectivePlan(null, Now).Should().Be(PlanTier.None);

    [Fact]
    public void Active_grants_its_plan()
        => PlanPolicy.EffectivePlan(Sub(PlanTier.Pro, SubscriptionStatus.Active), Now).Should().Be(PlanTier.Pro);

    [Fact]
    public void Trial_before_end_grants_plan()
    {
        var sub = Sub(PlanTier.Starter, SubscriptionStatus.Trialing, trialEnds: Now.AddDays(3));
        PlanPolicy.EffectivePlan(sub, Now).Should().Be(PlanTier.Starter);
    }

    [Fact]
    public void Trial_after_end_grants_none()
    {
        var sub = Sub(PlanTier.Pro, SubscriptionStatus.Trialing, trialEnds: Now.AddDays(-1));
        PlanPolicy.EffectivePlan(sub, Now).Should().Be(PlanTier.None);
    }

    [Fact]
    public void PastDue_within_grace_retains_access()
    {
        var sub = Sub(PlanTier.Pro, SubscriptionStatus.PastDue, periodEnds: Now.AddDays(-2));
        PlanPolicy.EffectivePlan(sub, Now).Should().Be(PlanTier.Pro);
    }

    [Fact]
    public void PastDue_beyond_grace_loses_access()
    {
        var sub = Sub(PlanTier.Pro, SubscriptionStatus.PastDue, periodEnds: Now.AddDays(-30));
        PlanPolicy.EffectivePlan(sub, Now).Should().Be(PlanTier.None);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Canceled)]
    [InlineData(SubscriptionStatus.IncompleteExpired)]
    [InlineData(SubscriptionStatus.Unpaid)]
    [InlineData(SubscriptionStatus.Paused)]
    [InlineData(SubscriptionStatus.None)]
    public void Terminal_statuses_grant_none(SubscriptionStatus status)
        => PlanPolicy.EffectivePlan(Sub(PlanTier.Pro, status), Now).Should().Be(PlanTier.None);

    private static Subscription Sub(
        PlanTier plan, SubscriptionStatus status, DateTime? trialEnds = null, DateTime? periodEnds = null)
        => new() { Plan = plan, Status = status, TrialEndsAt = trialEnds, CurrentPeriodEndsAt = periodEnds };
}
