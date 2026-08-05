using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Awards;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;

namespace OppSignal.Api.Controllers;

/// <summary>
/// Recompete Radar (Pro): incumbent contracts under the user's watched NAICS
/// codes whose period of performance ends soon — predicted rebids before
/// anything is posted on SAM.gov.
/// </summary>
[ApiController]
[Route("api/recompetes")]
[Authorize]
public sealed class RecompetesController : ControllerBase
{
    private readonly IRecompeteService _recompetes;
    private readonly IEntitlementService _entitlements;

    public RecompetesController(IRecompeteService recompetes, IEntitlementService entitlements)
    {
        _recompetes = recompetes;
        _entitlements = entitlements;
    }

    [HttpGet]
    public async Task<ActionResult<RecompetePage>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var entitlement = await _entitlements.GetAsync(userId, ct);
        if (!entitlement.Limits.CanSeeRecompetes)
            throw new PlanLimitException("Recompete Radar is a Pro feature. Upgrade to Pro to see expiring incumbent contracts before the rebid posts.");

        return Ok(await _recompetes.ListAsync(userId, page, pageSize, ct));
    }
}
