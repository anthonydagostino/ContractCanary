using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>
/// A user-defined saved search. A notice matches this profile when it satisfies
/// (NAICS OR PSC) AND every other set filter. Empty/null filter = no constraint.
/// Multi-valued filters are stored as Postgres <c>text[]</c> columns.
/// </summary>
public class MatchProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Owning user (ASP.NET Identity AppUser.Id).</summary>
    public Guid UserId { get; set; }

    public string Name { get; set; } = "My opportunities";

    /// <summary>Six-digit NAICS codes. Empty = don't filter on NAICS.</summary>
    public List<string> Naics { get; set; } = new();

    /// <summary>PSC / classification codes. Empty = don't filter on PSC.</summary>
    public List<string> Psc { get; set; } = new();

    /// <summary>Keywords matched (case-insensitive substring) against title + description. Empty = no keyword filter.</summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>Agency full-parent-path prefixes (e.g. "DEPT OF DEFENSE"). Prefix match. Empty = any agency.</summary>
    public List<string> AgencyPaths { get; set; } = new();

    /// <summary>Set-aside programs to include. Empty = any set-aside.</summary>
    public List<SetAsideCode> SetAsides { get; set; } = new();

    /// <summary>Two-letter place-of-performance states. Empty = any state.</summary>
    public List<string> States { get; set; } = new();

    /// <summary>Notice types to include. Empty = any type.</summary>
    public List<NoticeType> NoticeTypes { get; set; } = new();

    /// <summary>Inactive profiles are skipped by the matcher and produce no digest content.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Pro-plan flag: prioritize this profile's saved searches in ingest ordering.</summary>
    public bool IsPriority { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
