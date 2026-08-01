using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Application.Notices;
using OppSignal.Application.Saved;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/notices")]
[Authorize]
public sealed class NoticesController : ControllerBase
{
    private readonly INoticeService _notices;
    private readonly ISavedNoticeService _saved;
    private readonly IEntitlementService _entitlements;

    public NoticesController(INoticeService notices, ISavedNoticeService saved, IEntitlementService entitlements)
    {
        _notices = notices;
        _saved = saved;
        _entitlements = entitlements;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NoticeListItemDto>>> Search([FromQuery] NoticeQuery query, CancellationToken ct)
        => Ok(await _notices.SearchAsync(User.GetUserId(), query, ct));

    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> Stats(CancellationToken ct)
        => Ok(await _notices.GetStatsAsync(User.GetUserId(), ct));

    [HttpGet("{noticeId}")]
    public async Task<ActionResult<NoticeDetailDto>> Detail(string noticeId, CancellationToken ct)
        => Ok(await _notices.GetDetailAsync(User.GetUserId(), noticeId, ct));

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] NoticeQuery query, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var entitlement = await _entitlements.GetAsync(userId, ct);
        if (!entitlement.Limits.CanExportCsv)
            throw new PlanLimitException("CSV export is a Pro feature. Upgrade to Pro to export search results.");

        var csv = await _notices.ExportCsvAsync(userId, query, ct);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"oppsignal-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPut("{noticeId}/save")]
    public async Task<IActionResult> Save(string noticeId, [FromBody] SaveNoticeRequest? request, CancellationToken ct)
    {
        await _saved.SaveAsync(User.GetUserId(), noticeId, request?.Note, ct);
        return NoContent();
    }

    [HttpDelete("{noticeId}/save")]
    public async Task<IActionResult> Unsave(string noticeId, CancellationToken ct)
    {
        await _saved.UnsaveAsync(User.GetUserId(), noticeId, ct);
        return NoContent();
    }
}

public sealed record SaveNoticeRequest(string? Note);
