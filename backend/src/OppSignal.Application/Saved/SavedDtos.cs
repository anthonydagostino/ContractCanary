using OppSignal.Domain.Enums;

namespace OppSignal.Application.Saved;

/// <summary>A saved opportunity with its pursuit-pipeline stage, for the Saved page.</summary>
public sealed class SavedNoticeDto
{
    public string NoticeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? AgencyPath { get; set; }
    public string TypeLabel { get; set; } = "";
    public string? NaicsCode { get; set; }
    public SetAsideCode SetAside { get; set; }
    public string? SetAsideLabel { get; set; }
    public DateTime PostedDate { get; set; }
    public DateTime? ResponseDeadline { get; set; }
    public string? UiLink { get; set; }
    public string? Note { get; set; }
    public PipelineStatus Status { get; set; }
    public DateTime SavedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed record UpdateStatusRequest(PipelineStatus Status);
