using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Matching;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Ingest;

public interface IIngestService
{
    /// <param name="windowDaysOverride">Override the configured rolling window (used by the demo bootstrap to pull a wide window once).</param>
    Task<IngestRun> RunAsync(int? windowDaysOverride = null, CancellationToken ct = default);
}

/// <summary>
/// Source-agnostic ingest: pull the rolling window page by page, normalize,
/// idempotently upsert by NoticeId, then match new/updated notices. Audited in an
/// <see cref="IngestRun"/>. Idempotent — re-running never duplicates notices or matches.
/// </summary>
public sealed class IngestService : IIngestService
{
    private readonly IAppDbContext _db;
    private readonly ISamOpportunitiesClient _client;
    private readonly IMatchingService _matching;
    private readonly IClock _clock;
    private readonly IngestOptions _options;
    private readonly ILogger<IngestService> _log;

    public IngestService(
        IAppDbContext db,
        ISamOpportunitiesClient client,
        IMatchingService matching,
        IClock clock,
        IOptions<IngestOptions> options,
        ILogger<IngestService> log)
    {
        _db = db;
        _client = client;
        _matching = matching;
        _clock = clock;
        _options = options.Value;
        _log = log;
    }

    public async Task<IngestRun> RunAsync(int? windowDaysOverride = null, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var windowDays = windowDaysOverride ?? _options.WindowDays;
        var windowTo = DateOnly.FromDateTime(now);
        var windowFrom = windowTo.AddDays(-Math.Max(0, windowDays - 1));

        var run = new IngestRun
        {
            Source = _client.Source,
            Status = IngestStatus.Running,
            StartedAt = now,
            WindowFrom = windowFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            WindowTo = windowTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
        };
        _db.IngestRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        var changed = new Dictionary<string, Notice>();
        var amendments = new Dictionary<string, IReadOnlyList<DetectedChange>>();
        try
        {
            var pageSize = Math.Clamp(_options.PageSize, 1, 1000);
            var offset = 0;
            var pages = 0;

            while (pages < _options.MaxPagesPerRun)
            {
                var page = await _client.FetchPageAsync(windowFrom, windowTo, pageSize, offset, ct);
                pages++;
                run.PagesFetched = pages;

                if (page.Items.Count == 0) break;
                run.NoticesSeen += page.Items.Count;

                await UpsertPageAsync(page.Items, now, changed, amendments, run, ct);

                offset += pageSize;
                if (offset >= page.TotalRecords) break;
            }

            // Persist notice inserts/updates before matching.
            await _db.SaveChangesAsync(ct);

            run.MatchesCreated = await _matching.MatchNoticesAsync(changed.Values.ToList(), ct);

            // Raise change-alerts for users tracking any materially-changed notice.
            await CreateAmendmentAlertsAsync(amendments, now, ct);

            run.Status = IngestStatus.Succeeded;
            run.CompletedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Ingest {Source} ok: seen={Seen} inserted={Ins} updated={Upd} matches={Matches} pages={Pages}",
                run.Source, run.NoticesSeen, run.NoticesInserted, run.NoticesUpdated, run.MatchesCreated, run.PagesFetched);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ingest {Source} failed", run.Source);
            try
            {
                // The tracker may still hold the entities whose save just
                // failed (e.g. a DbUpdateException row); saving the failure
                // status with them attached would throw again and leave the
                // run stuck in Running forever.
                _db.ClearChangeTracker();
                run.Status = IngestStatus.Failed;
                run.Error = ex.Message;
                run.CompletedAt = _clock.UtcNow;
                _db.IngestRuns.Update(run);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception saveEx)
            {
                _log.LogError(saveEx, "Could not record ingest failure for run {RunId}", run.Id);
            }
        }

        return run;
    }

    private async Task UpsertPageAsync(
        IReadOnlyList<SamOpportunityDto> items, DateTime now,
        Dictionary<string, Notice> changed, Dictionary<string, IReadOnlyList<DetectedChange>> amendments,
        IngestRun run, CancellationToken ct)
    {
        // Dedup within page and against notices already touched this run.
        var pageIds = items.Select(i => i.NoticeId)
            .Where(id => !string.IsNullOrWhiteSpace(id) && !changed.ContainsKey(id))
            .Distinct().ToList();

        var existing = await _db.Notices
            .Where(n => pageIds.Contains(n.NoticeId))
            .ToDictionaryAsync(n => n.NoticeId, ct);

        foreach (var dto in items)
        {
            if (string.IsNullOrWhiteSpace(dto.NoticeId) || changed.ContainsKey(dto.NoticeId)) continue;
            var candidate = NoticeNormalizer.ToNotice(dto, now);

            if (!existing.TryGetValue(dto.NoticeId, out var current))
            {
                _db.Notices.Add(candidate);
                run.NoticesInserted++;
                changed[candidate.NoticeId] = candidate;
            }
            else if (!string.Equals(current.RawJson, candidate.RawJson, StringComparison.Ordinal))
            {
                // Detect material changes against the PRIOR stored values, before overwriting.
                var detected = NoticeChangeDetector.Detect(current, candidate);
                if (detected.Count > 0) amendments[current.NoticeId] = detected;

                CopyMutable(current, candidate);
                current.LastSeenAt = now;
                run.NoticesUpdated++;
                changed[current.NoticeId] = current;
            }
            else
            {
                current.LastSeenAt = now; // touch only
            }
        }
    }

    private async Task CreateAmendmentAlertsAsync(
        Dictionary<string, IReadOnlyList<DetectedChange>> amendments, DateTime now, CancellationToken ct)
    {
        if (amendments.Count == 0) return;
        var noticeIds = amendments.Keys.ToList();

        // Affected users = everyone tracking the notice: matched to a profile, or saved.
        var byNotice = new Dictionary<string, HashSet<Guid>>();
        void Track(string noticeId, Guid userId)
        {
            if (!byNotice.TryGetValue(noticeId, out var set)) byNotice[noticeId] = set = new HashSet<Guid>();
            set.Add(userId);
        }

        foreach (var mu in await _db.NoticeMatches.AsNoTracking()
                     .Where(m => noticeIds.Contains(m.NoticeId))
                     .Select(m => new { m.NoticeId, m.UserId }).Distinct().ToListAsync(ct))
            Track(mu.NoticeId, mu.UserId);

        foreach (var su in await _db.SavedNotices.AsNoTracking()
                     .Where(s => noticeIds.Contains(s.NoticeId))
                     .Select(s => new { s.NoticeId, s.UserId }).Distinct().ToListAsync(ct))
            Track(su.NoticeId, su.UserId);

        var created = 0;
        foreach (var (noticeId, changes) in amendments)
        {
            if (!byNotice.TryGetValue(noticeId, out var users)) continue;
            foreach (var userId in users)
                foreach (var change in changes)
                {
                    _db.NoticeAlerts.Add(new NoticeAlert
                    {
                        UserId = userId,
                        NoticeId = noticeId,
                        Type = change.Type,
                        Message = change.Message,
                        CreatedAt = now,
                    });
                    created++;
                }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Created {Count} change-alerts across {Notices} amended notices", created, amendments.Count);
        }
    }

    internal static void CopyMutable(Notice target, Notice src)
    {
        target.Title = src.Title;
        target.SolicitationNumber = src.SolicitationNumber;
        target.Type = src.Type;
        target.BaseType = src.BaseType;
        target.AgencyPath = src.AgencyPath;
        target.DepartmentName = src.DepartmentName;
        target.SubTierName = src.SubTierName;
        target.OfficeName = src.OfficeName;
        target.NaicsCode = src.NaicsCode;
        target.PscCode = src.PscCode;
        target.SetAside = src.SetAside;
        target.SetAsideDescription = src.SetAsideDescription;
        target.PostedDate = src.PostedDate;
        target.ResponseDeadline = src.ResponseDeadline;
        target.ArchiveDate = src.ArchiveDate;
        target.PopState = src.PopState;
        target.PopCity = src.PopCity;
        target.PopZip = src.PopZip;
        target.PopCountry = src.PopCountry;
        target.UiLink = src.UiLink;
        target.DescriptionLink = src.DescriptionLink;
        // The live list API only carries a description LINK; the resolved text
        // arrives separately. Copy when present so keyword matching and the
        // detail page don't drift from RawJson, but never clobber stored text
        // with null.
        if (src.Description is not null) target.Description = src.Description;
        target.PrimaryContactName = src.PrimaryContactName;
        target.PrimaryContactEmail = src.PrimaryContactEmail;
        target.PrimaryContactPhone = src.PrimaryContactPhone;
        target.IsActive = src.IsActive;
        target.RawJson = src.RawJson;
        target.SourceUpdatedAt = src.SourceUpdatedAt;
    }
}
