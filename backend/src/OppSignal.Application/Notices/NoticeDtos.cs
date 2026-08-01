using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Application.Notices;

/// <summary>Server-side search/filter/paging parameters for the opportunity table.</summary>
public sealed class NoticeQuery
{
    public string? Search { get; set; }               // free text over title + description
    public List<string> Naics { get; set; } = new();
    public List<string> Psc { get; set; } = new();
    public List<SetAsideCode> SetAsides { get; set; } = new();
    public List<string> States { get; set; } = new();
    public List<NoticeType> NoticeTypes { get; set; } = new();
    public string? Agency { get; set; }
    public DateTime? PostedFrom { get; set; }
    public DateTime? PostedTo { get; set; }
    public bool? ActiveOnly { get; set; } = true;

    /// <summary>Restrict to notices matched to the caller's profiles.</summary>
    public bool MatchedOnly { get; set; }
    /// <summary>Restrict to notices matched to a specific profile of the caller.</summary>
    public Guid? ProfileId { get; set; }
    /// <summary>Restrict to the caller's saved notices.</summary>
    public bool SavedOnly { get; set; }

    public string Sort { get; set; } = "posted";     // "posted" | "deadline"
    public string Direction { get; set; } = "desc";   // "asc" | "desc"

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public class NoticeListItemDto
{
    public string NoticeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? SolicitationNumber { get; set; }
    public NoticeType Type { get; set; }
    public string TypeLabel { get; set; } = "";
    public string? AgencyPath { get; set; }
    public string? DepartmentName { get; set; }
    public string? NaicsCode { get; set; }
    public string? PscCode { get; set; }
    public SetAsideCode SetAside { get; set; }
    public string? SetAsideLabel { get; set; }
    public DateTime PostedDate { get; set; }
    public DateTime? ResponseDeadline { get; set; }
    public string? PopState { get; set; }
    public string? PopCity { get; set; }
    public string? UiLink { get; set; }
    public bool IsSaved { get; set; }
    public bool IsMatched { get; set; }
}

public sealed class NoticeDetailDto : NoticeListItemDto
{
    public string? BaseType { get; set; }
    public string? SubTierName { get; set; }
    public string? OfficeName { get; set; }
    public string? SetAsideDescription { get; set; }
    public DateTime? ArchiveDate { get; set; }
    public string? PopZip { get; set; }
    public string? PopCountry { get; set; }
    public string? Description { get; set; }
    public string? DescriptionLink { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public bool IsActive { get; set; }
    public List<MatchedProfileDto> MatchedProfiles { get; set; } = new();
    public string? SavedNote { get; set; }

    // AI-generated overview (shared across users; null until enriched).
    public string? AiSummary { get; set; }
    public List<string> AiKeyPoints { get; set; } = new();
    public string? AiFitNote { get; set; }
    public string? AiModel { get; set; }
    public DateTime? AiGeneratedAt { get; set; }
}

public sealed record MatchedProfileDto(Guid ProfileId, string ProfileName, string? MatchReason);
