using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;

namespace OppSignal.Application.Ai;

public sealed record EnrichmentRunResult(int Considered, int Succeeded, int Failed)
{
    public static readonly EnrichmentRunResult Skipped = new(0, 0, 0);
}

public interface ISummaryEnrichmentService
{
    /// <summary>Summarize a batch of not-yet-summarized notices. Safe no-op when disabled.</summary>
    Task<EnrichmentRunResult> RunAsync(CancellationToken ct = default);
}

/// <summary>
/// Generates AI summaries for notices that don't have one yet. Runs once per notice
/// (the summary is shared across all users), newest-first, bounded per pass. On a
/// failure it records the attempt so a persistently-bad notice can't burn the budget.
/// </summary>
public sealed class SummaryEnrichmentService : ISummaryEnrichmentService
{
    private readonly IAppDbContext _db;
    private readonly IOpportunitySummarizer _summarizer;
    private readonly IClock _clock;
    private readonly AiOptions _options;
    private readonly ILogger<SummaryEnrichmentService> _log;

    public SummaryEnrichmentService(
        IAppDbContext db, IOpportunitySummarizer summarizer, IClock clock,
        IOptions<AiOptions> options, ILogger<SummaryEnrichmentService> log)
    {
        _db = db;
        _summarizer = summarizer;
        _clock = clock;
        _options = options.Value;
        _log = log;
    }

    public async Task<EnrichmentRunResult> RunAsync(CancellationToken ct = default)
    {
        if (!_summarizer.Enabled)
            return EnrichmentRunResult.Skipped;

        var batchSize = Math.Clamp(_options.MaxNoticesPerRun, 1, 200);
        var maxAttempts = Math.Max(1, _options.MaxAttempts);

        var pending = await _db.Notices
            .Where(n => n.AiGeneratedAt == null && n.IsActive && n.AiAttempts < maxAttempts)
            .OrderByDescending(n => n.PostedDate)
            .Take(batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return EnrichmentRunResult.Skipped;

        int ok = 0, failed = 0;
        foreach (var notice in pending)
        {
            ct.ThrowIfCancellationRequested();
            notice.AiAttempts++;
            try
            {
                var summary = await _summarizer.SummarizeAsync(notice, ct);
                if (summary is null)
                {
                    failed++;
                    continue;
                }

                notice.AiSummary = summary.Summary;
                notice.AiKeyPoints = summary.KeyPoints.Count > 0 ? string.Join('\n', summary.KeyPoints) : null;
                notice.AiFitNote = string.IsNullOrWhiteSpace(summary.FitNote) ? null : summary.FitNote;
                notice.AiModel = summary.Model;
                notice.AiGeneratedAt = _clock.UtcNow;
                ok++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                _log.LogWarning(ex, "AI summary failed for notice {NoticeId}", notice.NoticeId);
            }
        }

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("AI enrichment: considered={Considered} ok={Ok} failed={Failed}", pending.Count, ok, failed);
        return new EnrichmentRunResult(pending.Count, ok, failed);
    }
}
