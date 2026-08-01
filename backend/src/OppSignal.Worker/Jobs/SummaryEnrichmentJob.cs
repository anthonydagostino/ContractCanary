using OppSignal.Application.Ai;
using Quartz;

namespace OppSignal.Worker.Jobs;

/// <summary>Scheduled AI enrichment: summarize notices that don't have a summary yet.</summary>
[DisallowConcurrentExecution]
public sealed class SummaryEnrichmentJob : IJob
{
    private readonly ISummaryEnrichmentService _enrichment;
    private readonly ILogger<SummaryEnrichmentJob> _log;

    public SummaryEnrichmentJob(ISummaryEnrichmentService enrichment, ILogger<SummaryEnrichmentJob> log)
    {
        _enrichment = enrichment;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var result = await _enrichment.RunAsync(context.CancellationToken);
        if (result.Considered > 0)
            _log.LogInformation("SummaryEnrichmentJob: considered={Considered} ok={Ok} failed={Failed}",
                result.Considered, result.Succeeded, result.Failed);
    }
}
