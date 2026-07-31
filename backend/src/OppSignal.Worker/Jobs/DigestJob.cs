using Microsoft.Extensions.Options;
using OppSignal.Infrastructure.Email;
using OppSignal.Worker.Configuration;
using Quartz;

namespace OppSignal.Worker.Jobs;

/// <summary>
/// Runs hourly; sends the daily digest to each user whose local time just crossed
/// the configured send hour. Per-user timezone math + hourly tick = one digest
/// per user per day in their own timezone.
/// </summary>
[DisallowConcurrentExecution]
public sealed class DigestJob : IJob
{
    private readonly IDigestService _digest;
    private readonly DigestOptions _options;
    private readonly ILogger<DigestJob> _log;

    public DigestJob(IDigestService digest, IOptions<DigestOptions> options, ILogger<DigestJob> log)
    {
        _digest = digest;
        _options = options.Value;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var sent = await _digest.SendDueDigestsAsync(_options.SendHourLocal, context.CancellationToken);
        if (sent > 0) _log.LogInformation("DigestJob sent {Count} digests", sent);
    }
}
