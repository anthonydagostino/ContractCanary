using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Infrastructure.Auth;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Api.Controllers;

/// <summary>
/// No-login email unsubscribe (CAN-SPAM). The link carries a keyed token so it
/// can't be forged and the recipient never has to sign in. The GET only renders
/// a confirmation form — corporate mail scanners (Outlook SafeLinks, Mimecast)
/// prefetch every link in delivered mail, and a state-changing GET would let a
/// scanner silently unsubscribe the user. The POST performs the opt-out.
/// </summary>
[ApiController]
[Route("api")]
public sealed class UnsubscribeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UnsubscribeTokenService _tokens;
    private readonly IClock _clock;

    public UnsubscribeController(AppDbContext db, UnsubscribeTokenService tokens, IClock clock)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
    }

    [HttpGet("unsubscribe")]
    [AllowAnonymous]
    public IActionResult Confirm([FromQuery] string? u, [FromQuery] string? t)
    {
        var safeU = System.Net.WebUtility.HtmlEncode(u ?? "");
        var safeT = System.Net.WebUtility.HtmlEncode(t ?? "");
        var html = ConfirmFormHtml
            .Replace("{{U}}", safeU)
            .Replace("{{T}}", safeT);
        return Content(html, "text/html");
    }

    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    public async Task<IActionResult> Unsubscribe([FromForm] string? u, [FromForm] string? t, CancellationToken ct)
    {
        if (Guid.TryParse(u, out var userId) && _tokens.Validate(userId, t))
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user is not null && user.DigestOptedOutAt is null)
            {
                user.DigestOptedOutAt = _clock.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        // Always show the same confirmation (never reveal whether the token was valid).
        return Content(ConfirmationHtml, "text/html");
    }

    private const string PageOpen =
        "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
        "<meta name=\"robots\" content=\"noindex\">" +
        "<title>Unsubscribe</title></head>" +
        "<body style=\"font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;background:#f1f5f9;margin:0;padding:48px 16px;text-align:center;color:#0f172a\">" +
        "<div style=\"max-width:440px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:12px;padding:32px\">";

    private const string PageClose = "</div></body></html>";

    private const string ConfirmFormHtml = PageOpen +
        "<h1 style=\"font-size:20px;margin:0 0 8px\">Unsubscribe from the daily digest?</h1>" +
        "<p style=\"font-size:14px;color:#475569;line-height:1.6;margin:0 0 20px\">You’ll stop receiving the daily opportunity digest. " +
        "Account and billing emails (like receipts and security notices) are unaffected.</p>" +
        "<form method=\"post\" action=\"/api/unsubscribe\">" +
        "<input type=\"hidden\" name=\"u\" value=\"{{U}}\">" +
        "<input type=\"hidden\" name=\"t\" value=\"{{T}}\">" +
        "<button type=\"submit\" style=\"background:#0f172a;color:#fff;border:0;border-radius:8px;padding:10px 20px;font-size:14px;font-weight:600;cursor:pointer\">Unsubscribe</button>" +
        "</form>" + PageClose;

    private const string ConfirmationHtml = PageOpen +
        "<h1 style=\"font-size:20px;margin:0 0 8px\">You’re unsubscribed</h1>" +
        "<p style=\"font-size:14px;color:#475569;line-height:1.6;margin:0\">You will no longer receive the daily opportunity digest. " +
        "Account and billing emails (like receipts and security notices) are unaffected. " +
        "To start receiving the digest again, just contact our support team.</p>" +
        PageClose;
}
