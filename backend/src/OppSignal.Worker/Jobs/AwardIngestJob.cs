using OppSignal.Application.Awards;
using Quartz;

namespace OppSignal.Worker.Jobs;

/// <summary>Daily Recompete Radar pull from the award data source. Non-overlapping.</summary>
[DisallowConcurrentExecution]
public sealed class AwardIngestJob : IJob
{
    private readonly IAwardIngestService _awards;
    private readonly ILogger<AwardIngestJob> _log;

    public AwardIngestJob(IAwardIngestService awards, ILogger<AwardIngestJob> log)
    {
        _awards = awards;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _log.LogInformation("AwardIngestJob starting");
        var touched = await _awards.RunAsync(context.CancellationToken);
        _log.LogInformation("AwardIngestJob done: {Count} awards inserted/updated", touched);
    }
}
