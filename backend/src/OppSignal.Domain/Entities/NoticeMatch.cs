namespace OppSignal.Domain.Entities;

/// <summary>
/// A (notice × profile) match. Unique on (<see cref="NoticeId"/>,
/// <see cref="MatchProfileId"/>) so a notice can only match a profile once —
/// this is the dedup guarantee. <see cref="NotifiedAt"/> is stamped when the
/// match is included in a digest, so a user is never emailed twice for the same
/// (notice, profile).
/// </summary>
public class NoticeMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NoticeId { get; set; } = default!;
    public Notice? Notice { get; set; }

    public Guid MatchProfileId { get; set; }
    public MatchProfile? MatchProfile { get; set; }

    /// <summary>Denormalized for fast per-user digest queries without a join through the profile.</summary>
    public Guid UserId { get; set; }

    public DateTime MatchedAt { get; set; }

    /// <summary>Null until included in a digest email. Guarantees no double-notify.</summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>JSON describing which filters fired (for the detail view / debugging).</summary>
    public string? MatchReason { get; set; }
}
