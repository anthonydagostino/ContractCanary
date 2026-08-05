using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Admin;
using OppSignal.Application.Awards;
using OppSignal.Application.Ingest;
using OppSignal.Infrastructure.Email;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminMetricsService _metrics;
    private readonly IIngestService _ingest;
    private readonly IDigestService _digest;
    private readonly IAwardIngestService _awards;

    public AdminController(IAdminMetricsService metrics, IIngestService ingest, IDigestService digest, IAwardIngestService awards)
    {
        _metrics = metrics;
        _ingest = ingest;
        _digest = digest;
        _awards = awards;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<AdminMetricsDto>> Metrics(CancellationToken ct)
        => Ok(await _metrics.GetAsync(ct));

    /// <summary>Manually trigger an ingest run (ops / demo).</summary>
    [HttpPost("ingest/run")]
    public async Task<IActionResult> RunIngest([FromQuery] int? windowDays, CancellationToken ct)
    {
        var run = await _ingest.RunAsync(windowDays, ct);
        return Ok(new { run.Status, run.NoticesInserted, run.NoticesUpdated, run.MatchesCreated });
    }

    /// <summary>Manually trigger a Recompete Radar award pull (ops / demo).</summary>
    [HttpPost("awards/ingest")]
    public async Task<IActionResult> RunAwardIngest(CancellationToken ct)
    {
        var touched = await _awards.RunAsync(ct);
        return Ok(new { awardsUpserted = touched });
    }

    /// <summary>Send the calling admin their own digest now (demo/testing).</summary>
    [HttpPost("digest/send-me")]
    public async Task<IActionResult> SendMyDigest(CancellationToken ct)
    {
        var sent = await _digest.SendUserDigestAsync(User.GetUserId(), ct);
        return Ok(new { sent });
    }
}
