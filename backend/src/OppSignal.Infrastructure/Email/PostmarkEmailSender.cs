using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Common;
using OppSignal.Application.Email;

namespace OppSignal.Infrastructure.Email;

/// <summary>
/// Postmark transactional email transport. Configured purely by env vars
/// (Email__Postmark__ServerToken, Email__FromEmail, …). POSTs to the Postmark
/// /email endpoint with the server token header.
/// </summary>
public sealed class PostmarkEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly BrandingOptions _branding;
    private readonly ILogger<PostmarkEmailSender> _log;

    public PostmarkEmailSender(
        HttpClient http, IOptions<EmailOptions> options, IOptions<BrandingOptions> branding, ILogger<PostmarkEmailSender> log)
    {
        _http = http;
        _options = options.Value;
        _branding = branding.Value;
        _log = log;
    }

    public string Provider => "Postmark";

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Postmark.ServerToken))
            return new EmailSendResult(false, Provider, null, "Postmark server token not configured.");

        var fromEmail = _options.FromEmail ?? _branding.FromEmail;
        var fromName = _options.FromName ?? _branding.FromName;

        var payload = new
        {
            From = $"{fromName} <{fromEmail}>",
            To = message.ToName is null ? message.ToAddress : $"{message.ToName} <{message.ToAddress}>",
            message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            MessageStream = _options.Postmark.MessageStream,
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.TryAddWithoutValidation("X-Postmark-Server-Token", _options.Postmark.ServerToken);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("Postmark send failed ({Status}): {Body}", (int)response.StatusCode, body);
                return new EmailSendResult(false, Provider, null, $"Postmark {(int)response.StatusCode}: {body}");
            }

            string? messageId = null;
            try { messageId = JsonDocument.Parse(body).RootElement.GetProperty("MessageID").GetString(); }
            catch { /* best-effort */ }

            return new EmailSendResult(true, Provider, messageId, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Postmark send threw for {To}", message.ToAddress);
            return new EmailSendResult(false, Provider, null, ex.Message);
        }
    }
}
