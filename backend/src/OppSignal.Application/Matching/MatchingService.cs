using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Matching;

/// <summary>
/// Persists matches with dedup. Two entry points:
/// <list type="bullet">
/// <item><see cref="MatchNoticesAsync"/> — called by ingest for new/updated
/// notices; creates un-notified matches (they will be emailed in the next digest).</item>
/// <item><see cref="BackfillProfileAsync"/> — called when a profile is created or
/// edited; (re)creates matches over existing notices, pre-marked as notified so a
/// backlog is browsable in-app but never blasted out as a giant first email.</item>
/// </list>
/// Dedup is enforced by checking existing (NoticeId, ProfileId) pairs and by the
/// unique DB index.
/// </summary>
public sealed class MatchingService : IMatchingService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IAppDbContext _db;
    private readonly IMatchEngine _engine;
    private readonly IClock _clock;

    public MatchingService(IAppDbContext db, IMatchEngine engine, IClock clock)
    {
        _db = db;
        _engine = engine;
        _clock = clock;
    }

    /// <summary>Match the given (new/updated) notices against all active profiles.</summary>
    public async Task<int> MatchNoticesAsync(IReadOnlyCollection<Notice> notices, CancellationToken ct = default)
    {
        if (notices.Count == 0) return 0;

        var profiles = await _db.MatchProfiles.Where(p => p.IsActive).ToListAsync(ct);
        if (profiles.Count == 0) return 0;

        var noticeIds = notices.Select(n => n.NoticeId).ToList();
        var existing = await _db.NoticeMatches
            .Where(m => noticeIds.Contains(m.NoticeId))
            .Select(m => new { m.NoticeId, m.MatchProfileId })
            .ToListAsync(ct);
        var seen = existing.Select(e => (e.NoticeId, e.MatchProfileId)).ToHashSet();

        var now = _clock.UtcNow;
        var created = 0;
        foreach (var notice in notices)
        {
            foreach (var profile in profiles)
            {
                if (seen.Contains((notice.NoticeId, profile.Id))) continue;
                var outcome = _engine.Evaluate(notice, profile);
                if (!outcome.IsMatch) continue;

                _db.NoticeMatches.Add(new NoticeMatch
                {
                    NoticeId = notice.NoticeId,
                    MatchProfileId = profile.Id,
                    UserId = profile.UserId,
                    MatchedAt = now,
                    NotifiedAt = null,
                    MatchReason = JsonSerializer.Serialize(outcome.Reason, Json),
                });
                seen.Add((notice.NoticeId, profile.Id));
                created++;
            }
        }

        if (created > 0) await _db.SaveChangesAsync(ct);
        return created;
    }

    /// <summary>
    /// Rebuild all matches for a single profile over currently-active notices.
    /// Existing matches for the profile are cleared first (filters may have
    /// changed). New matches are pre-marked notified so they don't flood a digest.
    /// </summary>
    public Task<int> BackfillProfileAsync(Guid profileId, Guid userId, CancellationToken ct = default)
        => BackfillProfileAsync(profileId, userId, batchSize: 500, ct);

    // batchSize is overridable so tests can exercise batch-boundary behavior
    // without seeding hundreds of notices.
    internal async Task<int> BackfillProfileAsync(Guid profileId, Guid userId, int batchSize, CancellationToken ct)
    {
        // Scope by userId: a caller can never rebuild/delete matches for a profile it doesn't own.
        var profile = await _db.MatchProfiles.FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId, ct);
        if (profile is null || !profile.IsActive) return 0;

        var stale = await _db.NoticeMatches.Where(m => m.MatchProfileId == profileId && m.UserId == userId).ToListAsync(ct);
        if (stale.Count > 0) _db.NoticeMatches.RemoveRange(stale);

        var now = _clock.UtcNow;
        var created = 0;

        // Stream active notices in batches to bound memory at scale. PostedDate
        // is date-only and massively tied, so a unique tiebreak is required —
        // without it successive Skip/Take pages can repeat or drop rows, and a
        // repeated row would violate the (NoticeId, MatchProfileId) unique index.
        var seenNoticeIds = new HashSet<string>(StringComparer.Ordinal);
        var offset = 0;
        while (true)
        {
            var batch = await _db.Notices
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.PostedDate)
                .ThenBy(n => n.NoticeId)
                .Skip(offset).Take(batchSize)
                .ToListAsync(ct);
            if (batch.Count == 0) break;

            foreach (var notice in batch)
            {
                // Belt and braces: concurrent ingest can still shift offsets
                // between pages, so never add the same notice twice in one run.
                if (!seenNoticeIds.Add(notice.NoticeId)) continue;
                var outcome = _engine.Evaluate(notice, profile);
                if (!outcome.IsMatch) continue;
                _db.NoticeMatches.Add(new NoticeMatch
                {
                    NoticeId = notice.NoticeId,
                    MatchProfileId = profile.Id,
                    UserId = profile.UserId,
                    MatchedAt = now,
                    NotifiedAt = now, // backlog: browsable, not emailed
                    MatchReason = JsonSerializer.Serialize(outcome.Reason, Json),
                });
                created++;
            }
            offset += batchSize;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }
}
