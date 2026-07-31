using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Profiles;

/// <summary>Create/update payload for a match profile.</summary>
public sealed class ProfileInput
{
    public string Name { get; set; } = "";
    public List<string> Naics { get; set; } = new();
    public List<string> Psc { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<string> AgencyPaths { get; set; } = new();
    public List<SetAsideCode> SetAsides { get; set; } = new();
    public List<string> States { get; set; } = new();
    public List<NoticeType> NoticeTypes { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsPriority { get; set; }
}

/// <summary>Match profile projection returned to the client.</summary>
public sealed class MatchProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Naics { get; set; } = new();
    public List<string> Psc { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<string> AgencyPaths { get; set; } = new();
    public List<SetAsideCode> SetAsides { get; set; } = new();
    public List<string> States { get; set; } = new();
    public List<NoticeType> NoticeTypes { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsPriority { get; set; }
    public int MatchCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static MatchProfileDto From(MatchProfile p, int matchCount = 0) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Naics = p.Naics,
        Psc = p.Psc,
        Keywords = p.Keywords,
        AgencyPaths = p.AgencyPaths,
        SetAsides = p.SetAsides,
        States = p.States,
        NoticeTypes = p.NoticeTypes,
        IsActive = p.IsActive,
        IsPriority = p.IsPriority,
        MatchCount = matchCount,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
    };
}
