using OppSignal.Domain.Entities;

namespace OppSignal.Application.Awards;

/// <summary>
/// v2 SEAM — documented no-op. In v1 the <see cref="Award"/> table exists but is
/// never populated (see the explicit non-goals). The v2 award-history /
/// recompete-expiry feature will source from USAspending.gov and implement this
/// contract; the empty table + this stub keep that a drop-in addition with no
/// schema migration surprise.
///
/// Intentionally NOT registered in DI in v1.
/// </summary>
public interface IAwardIngestionStub
{
    /// <summary>Pull award records for a window and upsert into <see cref="Award"/>. No-op in v1.</summary>
    Task<int> IngestAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

/// <summary>The v1 no-op. Documents the seam without doing any work.</summary>
public sealed class NoOpAwardIngestionStub : IAwardIngestionStub
{
    public Task<int> IngestAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult(0); // v2: fetch USAspending.gov awards and upsert.
}
