using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>Audit record of every email the system sends.</summary>
public class EmailLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public string ToAddress { get; set; } = default!;

    public EmailKind Kind { get; set; }
    public string Subject { get; set; } = default!;

    public DateTime SentAt { get; set; }

    public string Provider { get; set; } = default!;   // "Dev" | "Postmark"
    public string? ProviderMessageId { get; set; }

    public bool Success { get; set; }
    public string? Error { get; set; }
}
