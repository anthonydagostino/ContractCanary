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

                await UpsertPageAsync(page.Items, now, changed, run, ct);

                offset += pageSize;
                if (offset >= page.TotalRecords) break;
            }

            // Persist notice inserts/updates before matching.
            await _db.SaveChangesAsync(ct);

            run.MatchesCreated = await _matching.MatchNoticesAsync(changed.Values.ToList(), ct);

            run.Status = IngestStatus.Succeeded;
            run.CompletedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Ingest {Source} ok: seen={Seen} inserted={Ins} updated={Upd} matches={Matches} pages={Pages}",
                run.Source, run.NoticesSeen, run.NoticesInserted, run.NoticesUpdated, run.MatchesCreated, run.PagesFetched);
        }
        catch (Exception ex)
        {
            run.Status = IngestStatus.Failed;
            run.Error = ex.Message;
            run.CompletedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
            _log.LogError(ex, "Ingest {Source} failed", run.Source);
        }

        return run;
    }

    private async Task UpsertPageAsync(
        IReadOnlyList<SamOpportunityDto> items, DateTime now,
        Dictionary<string, Notice> changed, IngestRun run, CancellationToken ct)
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

    private static void CopyMutable(Notice target, Notice src)
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
        target.PrimaryContactName = src.PrimaryContactName;
        target.PrimaryContactEmail = src.PrimaryContactEmail;
        target.PrimaryContactPhone = src.PrimaryContactPhone;
        target.IsActive = src.IsActive;
        target.RawJson = src.RawJson;
        target.SourceUpdatedAt = src.SourceUpdatedAt;
    }
}
