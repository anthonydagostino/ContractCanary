using OppSignal.Domain.Enums;

namespace OppSignal.Application.Email;

/// <summary>A ready-to-send email (already rendered).</summary>
public sealed class EmailMessage
{
    public required string ToAddress { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TextBody { get; init; }
    public EmailKind Kind { get; init; }
    public Guid? UserId { get; init; }
}

public sealed record EmailSendResult(bool Success, string Provider, string? ProviderMessageId, string? Error);

/// <summary>
/// Transport port. Two implementations: <c>DevEmailSender</c> (writes to disk +
/// console, zero credentials) and <c>PostmarkEmailSender</c> (env-configured).
/// </summary>
public interface IEmailSender
{
    string Provider { get; }
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}
