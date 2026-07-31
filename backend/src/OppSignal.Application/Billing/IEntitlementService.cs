using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Billing;

public sealed record Entitlement(PlanTier Plan, PlanLimits Limits, Subscription? Subscription);

public interface IEntitlementService
{
    Task<Entitlement> GetAsync(Guid userId, CancellationToken ct = default);
}

public sealed class EntitlementService : IEntitlementService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public EntitlementService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Entitlement> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var plan = PlanPolicy.EffectivePlan(sub, _clock.UtcNow);
        return new Entitlement(plan, PlanPolicy.LimitsFor(plan), sub);
    }
}
