using OppSignal.Application.Ingest;
using Quartz;

namespace OppSignal.Worker.Jobs;

/// <summary>Scheduled ingest pull + match. Non-overlapping.</summary>
[DisallowConcurrentExecution]
public sealed class IngestJob : IJob
{
    private readonly IIngestService _ingest;
    private readonly ILogger<IngestJob> _log;

    public IngestJob(IIngestService ingest, ILogger<IngestJob> log)
    {
        _ingest = ingest;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _log.LogInformation("IngestJob starting");
        var run = await _ingest.RunAsync(ct: context.CancellationToken);
        _log.LogInformation("IngestJob done: {Status} inserted={Ins} updated={Upd} matches={Matches}",
            run.Status, run.NoticesInserted, run.NoticesUpdated, run.MatchesCreated);
    }
}
