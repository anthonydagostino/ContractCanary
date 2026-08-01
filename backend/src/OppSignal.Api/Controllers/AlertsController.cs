using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Alerts;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _alerts;

    public AlertsController(IAlertService alerts) => _alerts = alerts;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> List(CancellationToken ct)
        => Ok(await _alerts.ListAsync(User.GetUserId(), ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken ct)
        => Ok(new { count = await _alerts.UnreadCountAsync(User.GetUserId(), ct) });

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await _alerts.MarkAllReadAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
