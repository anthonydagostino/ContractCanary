using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Common;
using OppSignal.Application.Email;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Infrastructure.Email;

/// <summary>
/// Composes templated notifications, renders responsive HTML via RazorLight,
/// dispatches through the configured <see cref="IEmailSender"/>, and audits every
/// send to <c>EmailLog</c>. The digest is skipped entirely on zero matches.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IEmailRenderer _renderer;
    private readonly IEmailSender _sender;
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly BrandingOptions _branding;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(
        IEmailRenderer renderer,
        IEmailSender sender,
        AppDbContext db,
        IClock clock,
        IOptions<BrandingOptions> branding,
        ILogger<NotificationService> log)
    {
        _renderer = renderer;
        _sender = sender;
        _db = db;
        _clock = clock;
        _branding = branding.Value;
        _log = log;
    }

    public async Task SendEmailVerificationAsync(Guid userId, string email, string? name, string verifyUrl, CancellationToken ct = default)
    {
        var model = Action("Confirm your email",
            $"Welcome to {_branding.ProductName}! Confirm your email address to activate your account and start receiving opportunity alerts.",
            "Confirm email", verifyUrl,
            "If you didn’t create this account, you can safely ignore this email.", name);
        await RenderSendLogAsync("Action", model,
            $"Confirm your {_branding.ProductName} email", EmailKind.Verification, userId, email, name, ct);
    }

    public async Task SendPasswordResetAsync(Guid userId, string email, string? name, string resetUrl, CancellationToken ct = default)
    {
        var model = Action("Reset your password",
            "We received a request to reset your password. Click below to choose a new one. This link expires shortly.",
            "Reset password", resetUrl,
            "If you didn’t request a password reset, you can safely ignore this email.", name);
        await RenderSendLogAsync("Action", model,
            $"Reset your {_branding.ProductName} password", EmailKind.PasswordReset, userId, email, name, ct);
    }

    public async Task SendWelcomeAsync(Guid userId, string email, string? name, CancellationToken ct = default)
    {
        var model = Action($"You’re all set on {_branding.ProductName}",
            "Your email is confirmed. Create a match profile with your NAICS codes, keywords, and target agencies — and we’ll email you the moment a matching opportunity is posted.",
            "Go to dashboard", $"{_branding.WebBaseUrl.TrimEnd('/')}/app",
            "Questions? Just reply to this email.", name);
        await RenderSendLogAsync("Action", model,
            $"Welcome to {_branding.ProductName}", EmailKind.Welcome, userId, email, name, ct);
    }

    public async Task SendAccountExistsAsync(Guid userId, string email, string? name, string signInUrl, CancellationToken ct = default)
    {
        var model = Action("You already have an account",
            $"Someone tried to sign up for {_branding.ProductName} using this email address, but an account already exists for it. If that was you, just sign in below — or reset your password if you’ve forgotten it.",
            "Sign in", signInUrl,
            "If this wasn’t you, no action is needed — your account is safe.", name);
        await RenderSendLogAsync("Action", model,
            $"You already have a {_branding.ProductName} account", EmailKind.Welcome, userId, email, name, ct);
    }

    public async Task<bool> SendDigestAsync(DigestModel model, CancellationToken ct = default)
    {
        // never send an empty digest
        if (model.TotalCount == 0 && model.AlertCount == 0 && model.ClosingSoonCount == 0) return false;

        ApplyBranding(model);
        var html = await _renderer.RenderAsync("Digest", model);
        var text = BuildDigestText(model);

        var subject = model.HasMatches
            ? (model.TotalCount == 1
                ? $"1 new contract opportunity — {model.DateLabel}"
                : $"{model.TotalCount} new contract opportunities — {model.DateLabel}")
            : model.HasAlerts
                ? (model.AlertCount == 1
                    ? $"An opportunity you're tracking changed — {model.DateLabel}"
                    : $"{model.AlertCount} opportunities you're tracking changed — {model.DateLabel}")
                : (model.ClosingSoonCount == 1
                    ? $"An opportunity you're tracking is closing soon — {model.DateLabel}"
                    : $"{model.ClosingSoonCount} opportunities you're tracking are closing soon — {model.DateLabel}");

        await SendAndLogAsync(new EmailMessage
        {
            ToAddress = model.ToEmail,
            ToName = model.ToName,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
            Kind = EmailKind.Digest,
            UserId = model.UserId,
        }, ct);
        return true;
    }

    // ---- helpers ------------------------------------------------------------

    private EmailActionModel Action(string heading, string intro, string button, string url, string outro, string? name)
        => new()
        {
            ProductName = _branding.ProductName,
            Tagline = _branding.Tagline,
            BrandColor = _branding.BrandColor,
            SupportEmail = _branding.SupportEmail,
            WebBaseUrl = _branding.WebBaseUrl,
            Name = name,
            Heading = heading,
            Intro = intro,
            ButtonText = button,
            ActionUrl = url,
            Outro = outro,
        };

    private void ApplyBranding(DigestModel model)
    {
        model.ProductName = _branding.ProductName;
        model.Tagline = _branding.Tagline;
        model.BrandColor = _branding.BrandColor;
        model.SupportEmail = _branding.SupportEmail;
        model.WebBaseUrl = _branding.WebBaseUrl;
    }

    private async Task RenderSendLogAsync(
        string template, EmailActionModel model, string subject, EmailKind kind,
        Guid userId, string email, string? name, CancellationToken ct)
    {
        var html = await _renderer.RenderAsync(template, model);
        var text = $"{model.Heading}\n\n{model.Intro}\n\n{model.ButtonText}: {model.ActionUrl}\n\n{model.Outro}";
        await SendAndLogAsync(new EmailMessage
        {
            ToAddress = email, ToName = name, Subject = subject,
            HtmlBody = html, TextBody = text, Kind = kind, UserId = userId,
        }, ct);
    }

    private async Task SendAndLogAsync(EmailMessage message, CancellationToken ct)
    {
        var result = await _sender.SendAsync(message, ct);
        _db.EmailLogs.Add(new EmailLog
        {
            UserId = message.UserId,
            ToAddress = message.ToAddress,
            Kind = message.Kind,
            Subject = message.Subject,
            SentAt = _clock.UtcNow,
            Provider = result.Provider,
            ProviderMessageId = result.ProviderMessageId,
            Success = result.Success,
            Error = result.Error,
        });
        await _db.SaveChangesAsync(ct);

        if (!result.Success)
            _log.LogWarning("Email {Kind} to {To} failed: {Error}", message.Kind, message.ToAddress, result.Error);
    }

    private static string BuildDigestText(DigestModel model)
    {
        var headline = model.HasMatches
            ? $"{model.TotalCount} new opportunit{(model.TotalCount == 1 ? "y" : "ies")}"
            : model.HasAlerts
                ? $"{model.AlertCount} update{(model.AlertCount == 1 ? "" : "s")} on opportunities you're tracking"
                : $"{model.ClosingSoonCount} opportunit{(model.ClosingSoonCount == 1 ? "y" : "ies")} closing soon";
        var lines = new List<string>
        {
            $"{model.ProductName} — {headline} ({model.DateLabel})",
            "",
        };
        if (model.HasClosingSoon)
        {
            lines.Add($"== Closing soon ({model.ClosingSoonCount}) ==");
            foreach (var c in model.ClosingSoon)
            {
                lines.Add($"- [{c.DaysLeftLabel}] {c.Title}");
                lines.Add($"  {c.Agency}");
                lines.Add($"  Response due: {c.DeadlineLabel}");
                lines.Add($"  Details: {c.DetailLink}");
                lines.Add("");
            }
        }
        if (model.HasAlerts)
        {
            lines.Add($"== Changes to opportunities you're tracking ({model.AlertCount}) ==");
            foreach (var alert in model.Alerts)
            {
                lines.Add($"- [{alert.TypeLabel}] {alert.Message}");
                lines.Add($"  {alert.Title}");
                lines.Add($"  Details: {alert.DetailLink}");
                lines.Add("");
            }
        }
        foreach (var group in model.Groups)
        {
            lines.Add($"== {group.ProfileName} ({group.Items.Count}) ==");
            foreach (var item in group.Items)
            {
                lines.Add($"- {item.Title}");
                lines.Add($"  {item.Agency} | {item.TypeLabel}{(item.SetAside is null ? "" : " | " + item.SetAside)}");
                if (item.Deadline is not null) lines.Add($"  Response due: {item.Deadline}");
                if (item.PlaceOfPerformance is not null) lines.Add($"  Place: {item.PlaceOfPerformance}");
                lines.Add($"  SAM.gov: {item.SamLink}");
                lines.Add($"  Details: {item.DetailLink}");
                lines.Add("");
            }
        }
        lines.Add($"Manage your alerts: {model.SettingsUrl}");
        return string.Join("\n", lines);
    }
}
