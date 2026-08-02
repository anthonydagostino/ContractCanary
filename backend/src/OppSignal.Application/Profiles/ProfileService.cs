using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Application.Matching;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Profiles;

public interface IProfileService
{
    Task<IReadOnlyList<MatchProfileDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<MatchProfileDto> GetAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<MatchProfileDto> CreateAsync(Guid userId, ProfileInput input, CancellationToken ct = default);
    Task<MatchProfileDto> UpdateAsync(Guid userId, Guid id, ProfileInput input, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}

/// <summary>
/// Profile CRUD with plan-limit enforcement. Creating a profile beyond the plan's
/// <c>MaxProfiles</c> throws <see cref="PlanLimitException"/>; setting priority
/// without the Pro entitlement is silently disallowed. Create/update trigger a
/// (re)backfill so matches reflect the current filters immediately.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly IAppDbContext _db;
    private readonly IEntitlementService _entitlements;
    private readonly IMatchingService _matching;
    private readonly IClock _clock;

    public ProfileService(IAppDbContext db, IEntitlementService entitlements, IMatchingService matching, IClock clock)
    {
        _db = db;
        _entitlements = entitlements;
        _matching = matching;
        _clock = clock;
    }

    public async Task<IReadOnlyList<MatchProfileDto>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var profiles = await _db.MatchProfiles
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var counts = await _db.NoticeMatches
            .Where(m => m.UserId == userId)
            .GroupBy(m => m.MatchProfileId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return profiles.Select(p => MatchProfileDto.From(p, counts.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<MatchProfileDto> GetAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var p = await Owned(userId, id, ct);
        var count = await _db.NoticeMatches.CountAsync(m => m.MatchProfileId == id, ct);
        return MatchProfileDto.From(p, count);
    }

    public async Task<MatchProfileDto> CreateAsync(Guid userId, ProfileInput input, CancellationToken ct = default)
    {
        var entitlement = await _entitlements.GetAsync(userId, ct);
        var current = await _db.MatchProfiles.CountAsync(p => p.UserId == userId, ct);
        if (current >= entitlement.Limits.MaxProfiles)
        {
            throw new PlanLimitException(entitlement.Limits.MaxProfiles == 0
                ? "Your trial has ended. Choose a plan to create match profiles."
                : $"Your {entitlement.Plan} plan allows {entitlement.Limits.MaxProfiles} profile(s). Upgrade to add more.");
        }

        var now = _clock.UtcNow;
        var profile = new MatchProfile { UserId = userId, CreatedAt = now, UpdatedAt = now };
        Apply(profile, input, entitlement.Limits.CanPrioritize);
        _db.MatchProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        var count = await _matching.BackfillProfileAsync(profile.Id, userId, ct);
        return MatchProfileDto.From(profile, count);
    }

    public async Task<MatchProfileDto> UpdateAsync(Guid userId, Guid id, ProfileInput input, CancellationToken ct = default)
    {
        var profile = await Owned(userId, id, ct);
        var entitlement = await _entitlements.GetAsync(userId, ct);

        Apply(profile, input, entitlement.Limits.CanPrioritize);
        profile.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        var count = await _matching.BackfillProfileAsync(profile.Id, userId, ct);
        return MatchProfileDto.From(profile, count);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var profile = await Owned(userId, id, ct);
        _db.MatchProfiles.Remove(profile);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<MatchProfile> Owned(Guid userId, Guid id, CancellationToken ct)
        => await _db.MatchProfiles.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct)
           ?? throw new NotFoundException("Profile not found.");

    private static void Apply(MatchProfile p, ProfileInput input, bool canPrioritize)
    {
        p.Name = string.IsNullOrWhiteSpace(input.Name) ? "Untitled profile" : input.Name.Trim();
        p.Naics = Clean(input.Naics);
        p.Psc = Clean(input.Psc);
        p.Keywords = Clean(input.Keywords);
        p.AgencyPaths = Clean(input.AgencyPaths);
        p.States = Clean(input.States).Select(s => s.ToUpperInvariant()).ToList();
        p.SetAsides = input.SetAsides.Distinct().ToList();
        p.NoticeTypes = input.NoticeTypes.Distinct().ToList();
        p.IsActive = input.IsActive;
        p.IsPriority = input.IsPriority && canPrioritize; // priority is a Pro feature
    }

    private static List<string> Clean(IEnumerable<string> values) => values
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
