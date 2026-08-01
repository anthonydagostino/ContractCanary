using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>
/// A normalized SAM.gov contract-opportunity notice. Natural key is
/// <see cref="NoticeId"/> (the SAM.gov notice id). The full upstream record is
/// retained in <see cref="RawJson"/> for forward-compatibility.
/// </summary>
public class Notice
{
    /// <summary>SAM.gov notice id — natural primary key.</summary>
    public string NoticeId { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? SolicitationNumber { get; set; }

    public NoticeType Type { get; set; } = NoticeType.Unknown;
    public string? BaseType { get; set; }

    // Agency hierarchy (from fullParentPathName, ".")-delimited top→office.
    public string? AgencyPath { get; set; }
    public string? DepartmentName { get; set; }
    public string? SubTierName { get; set; }
    public string? OfficeName { get; set; }

    public string? NaicsCode { get; set; }
    public string? PscCode { get; set; }        // classificationCode

    public SetAsideCode SetAside { get; set; } = SetAsideCode.None;
    public string? SetAsideDescription { get; set; }

    public DateTime PostedDate { get; set; }
    public DateTime? ResponseDeadline { get; set; }   // stored UTC
    public DateTime? ArchiveDate { get; set; }

    // Place of performance (flattened).
    public string? PopState { get; set; }
    public string? PopCity { get; set; }
    public string? PopZip { get; set; }
    public string? PopCountry { get; set; }

    public string? UiLink { get; set; }            // human sam.gov link
    public string? DescriptionLink { get; set; }   // API noticedesc url
    public string? Description { get; set; }        // resolved text (nullable)

    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Full upstream JSON record (jsonb).</summary>
    public string RawJson { get; set; } = "{}";

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    /// <summary>Upstream modified marker used to decide insert-vs-update on upsert.</summary>
    public DateTime? SourceUpdatedAt { get; set; }

    // ---- AI opportunity summary (generated once per notice, shared by all users) ----
    /// <summary>Plain-English overview of the opportunity. Null until enriched.</summary>
    public string? AiSummary { get; set; }
    /// <summary>Short bullet takeaways, newline-separated. Null until enriched.</summary>
    public string? AiKeyPoints { get; set; }
    /// <summary>One-line bid/no-bid fit consideration.</summary>
    public string? AiFitNote { get; set; }
    /// <summary>Model id that produced the summary (audit / cost tracking).</summary>
    public string? AiModel { get; set; }
    /// <summary>When the summary was generated (UTC). Null = not yet summarized.</summary>
    public DateTime? AiGeneratedAt { get; set; }
    /// <summary>Enrichment attempts so far; bounds retries on a notice that keeps failing.</summary>
    public int AiAttempts { get; set; }

    public ICollection<NoticeMatch> Matches { get; set; } = new List<NoticeMatch>();
}
