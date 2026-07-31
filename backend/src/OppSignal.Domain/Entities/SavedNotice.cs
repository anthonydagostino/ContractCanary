namespace OppSignal.Domain.Entities;

/// <summary>A user's starred notice. Unique on (UserId, NoticeId).</summary>
public class SavedNotice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string NoticeId { get; set; } = default!;
    public Notice? Notice { get; set; }

    public DateTime SavedAt { get; set; }

    public string? Note { get; set; }
}
