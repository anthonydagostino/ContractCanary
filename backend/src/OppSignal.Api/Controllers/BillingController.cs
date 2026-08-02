using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Billing;
using OppSignal.Application.Common;
using OppSignal.Domain.Enums;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize] // class-level default; the webhook opts out with [AllowAnonymous]
public sealed class BillingController : ControllerBase
{
    private readonly IBillingService _billing;

    public BillingController(IBillingService billing) => _billing = billing;

    [HttpGet("config")]
    [Authorize]
    public ActionResult<BillingConfig> Config() => Ok(_billing.GetConfig());

    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PlanTier>(request.Plan, ignoreCase: true, out var plan)
            || plan is PlanTier.None)
            throw new BadRequestException("Choose a valid plan (Starter or Pro).");

        var url = await _billing.CreateCheckoutSessionAsync(User.GetUserId(), plan, ct);
        return Ok(new { url });
    }

    [HttpPost("portal")]
    [Authorize]
    public async Task<IActionResult> Portal(CancellationToken ct)
    {
        var url = await _billing.CreatePortalSessionAsync(User.GetUserId(), ct);
        return Ok(new { url });
    }

    /// <summary>Stripe webhook. Reads the raw body and verifies the signature.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [RequestSizeLimit(262_144)] // cap the unauthenticated body (Stripe events are small)
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _billing.HandleWebhookAsync(payload, signature, ct);
        return Ok();
    }
}

public sealed record CheckoutRequest(string Plan);
