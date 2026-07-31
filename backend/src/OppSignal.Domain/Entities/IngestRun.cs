using OppSignal.Domain.Enums;

namespace OppSignal.Domain.Entities;

/// <summary>Audit record of a single ingest pull (window, counts, errors).</summary>
public class IngestRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public IngestSource Source { get; set; }
    public IngestStatus Status { get; set; } = IngestStatus.Running;

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime WindowFrom { get; set; }
    public DateTime WindowTo { get; set; }

    public int PagesFetched { get; set; }
    public int NoticesSeen { get; set; }
    public int NoticesInserted { get; set; }
    public int NoticesUpdated { get; set; }
    public int MatchesCreated { get; set; }

    public string? Error { get; set; }
}
