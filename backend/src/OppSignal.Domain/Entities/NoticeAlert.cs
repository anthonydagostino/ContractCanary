using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>
/// A per-user alert that an opportunity the user is tracking (matched or saved)
/// materially changed — the response deadline moved, or it was cancelled/archived.
/// Created by the ingest pipeline when it detects the change.
/// </summary>
public class NoticeAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string NoticeId { get; set; } = default!;
    public Notice? Notice { get; set; }

    public AlertType Type { get; set; }

    /// <summary>Human-readable one-liner (e.g. "Response deadline moved to Jul 15, 2026.").</summary>
    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    /// <summary>Null until the user opens their alerts.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Null until included in a digest email (reserved for a future email hook).</summary>
    public DateTime? NotifiedAt { get; set; }
}
